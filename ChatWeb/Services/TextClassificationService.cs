/*
AVA Chat Engine is a chat bot API.

Copyright (C) 2015-2019  Asurion, LLC

AVA Chat Engine is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

AVA Chat Engine is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with AVA Chat Engine.  If not, see <https://www.gnu.org/licenses/>.
*/

using Amazon.XRay.Recorder.Handlers.System.Net;
using ChatWeb.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChatWeb.Services
{
    public class StringTable
    {
        public string[] ColumnNames { get; set; }
        public string[,] Values { get; set; }
    }

    public class TextClassificationService
    {
        public const string IntentGatewayCategory = "intentgateway";
        public const string CommonChatCategory = "commonchat";
        static readonly char[] simpleTokenSplitter = new char[] { ' ', '\r', '\n', '.', '?', '!', ','};
        readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static readonly SHA1Managed sha1 = new SHA1Managed();

        readonly ClassifierConfigSectionCollection classifierConfigs;
        readonly IExternalDataStorageService AwsService;
        readonly MemCacheService memcacheService;
        readonly string configFileName;

        const string GreetingRegex = @"^((hi)|(hi there)|(hello)|(hello there))$"; // Make specific to only hi/hello so it doesnt skip the classifiers
        const string cacheVersion = "v2";

        public TextClassificationService(string configFilePath, IExternalDataStorageService awsService, MemCacheService memCacheLookup)
        {
            this.AwsService = awsService;
            this.memcacheService = memCacheLookup;

            configFileName = Path.GetFileName(configFilePath);
            classifierConfigs = ClassifierConfigSectionMappingConfigSection.GetClassifierConfigs(configFilePath);
        }

        /// <summary>
        /// Classifies text using IntentGateway and commonchat classifiers and combines the results
        /// </summary>
        /// <param name="intentGatewayService">Intent Gateway Service to use for classification</param>
        /// <param name="threshold">minimum threshold to be considered a valid classification</param>
        /// <param name="message">text to classify</param>
        /// <param name="ignoreCache">Flag to ignore the cached result for the phrase if available</param>
        /// <param name="classifierData">Data to pass to the intent gateway classifier</param>
        /// <param name="allowSmsClassifications">Set to true to allow classifications specific to SMS channel (ex: commonchat-Stop)</param>
        /// <returns></returns>
        public async Task<ClassificationResults> ClassifyAsync(IntentGatewayService intentGatewayService, double threshold, string message, bool ignoreCache, object classifierData, bool allowSmsClassifications)
        {
            return await ClassifyAsync(IntentGatewayCategory, intentGatewayService, threshold, message, ignoreCache, classifierData, allowSmsClassifications);
        }

        /// <summary>
        /// Classifies text using different classifiers and combines the results
        /// </summary>
        /// <param name="classifiers">Name of classifier to start with</param>
        /// <param name="threshold">minimum threshold to be considered a valid classification</param>
        /// <param name="message">text to classify</param>
        /// <param name="ignoreCache">Flag to ignore the cached result for the phrase if available</param>
        /// <param name="allowSmsClassifications">Set to true to allow classifications specific to SMS channel (ex: commonchat-Stop)</param>
        /// <returns></returns>
        public async Task<ClassificationResults> ClassifyAsync(string classifiers, double threshold, string message, bool ignoreCache, bool allowSmsClassifications)
        {
            return await ClassifyAsync(classifiers, null, threshold, message, ignoreCache, null, allowSmsClassifications);
        }

        async Task<ClassificationResults> ClassifyAsync(string classifiers, IntentGatewayService intentGatewayService, double threshold, string message, bool ignoreCache, object classifierData, bool allowSmsClassifications)
        {
            if (String.IsNullOrEmpty(message))
                return new ClassificationResults(0.0);

            string hash = CreateHashValue(message, classifiers, threshold, classifierData);

            if (!ignoreCache)
            {
                var cacheResult = CheckCacheResult(message, hash);
                if (cacheResult != null)
                    return cacheResult;
            }

            if (String.IsNullOrEmpty(classifiers))
                classifiers = "main";

            List<Task> tasks = new List<Task>();

            Task<ClassificationResponse[]> classifyTask = null;
            if (classifiers == IntentGatewayCategory)
                classifyTask = intentGatewayService.Classify(classifierData);
            else if (classifiers != CommonChatCategory)
                classifyTask = InternalClassify(classifiers, message, threshold);

            var commonTask = CheckForCommonChat(message, allowSmsClassifications);
            tasks.Add(commonTask);

            if (classifyTask != null)
                tasks.Add(classifyTask);

            await Task.WhenAll(tasks);

            var response = classifyTask?.Result;
            var commonResponse = commonTask.Result;

            ClassificationResults results = new ClassificationResults(threshold)
            {
                ClassifierResults = response,
                CommonChatResults = commonResponse
            };

            await UpdateReasons(results.TopResults);

            if (!ignoreCache && memcacheService != null && (response?.Length > 0) && (commonResponse?.Length > 0))
                memcacheService.SaveObject(hash, results, ChatConfiguration.ClassificationCacheTimeoutSeconds);

            return results;
        }

        private async Task UpdateReasons(ClassificationResponse[] topResponses)
        {
            if (AwsService != null)
            {
                foreach (var result in topResponses)
                {
                    // Intent is not set here for IntentGateway
                    if (!String.IsNullOrEmpty(result.Intent))
                    {
                        var reasonMap = await AwsService.GetChatReasonCategoryMap(result.Intent);
                        result.Reason = reasonMap?.reason;
                        result.Subreason = reasonMap?.subreason;
                    }
                }
            }
        }

        private ClassificationResults CheckCacheResult(string message, string hash)
        {
            if (memcacheService == null)
                return null;

            if (memcacheService != null)
            {
                var lookup = memcacheService.GetObject<ClassificationResults>(hash);
                if (lookup != null)
                {
                    logger.DebugFormat("Internal: Classification cache hit for {0}", message);

                    return lookup;
                }
            }

            return null;
        }

        private string CreateHashValue(string message, string classifiers, double threshold, object classifierData)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(message);
            sb.Append(classifiers);
            sb.Append(threshold);
            sb.Append(configFileName);
            sb.Append(cacheVersion);
            sb.Append(JsonConvert.SerializeObject(classifierData));

            return Hash(sb.ToString());
        }

        async Task<ClassificationResponse[]> InternalClassify(string category, string message, double threshold)
        {
            List<ClassificationResponse> results = new List<ClassificationResponse>();

            var classifications = await ClassifyText(category, message);

            if (classifications != null)
            {
                // Use more specific parsed topic, if the classification topic
                // is empty, or if the classification general category matches the parsed general category
                for (int i = 0; i < classifications.Length; i++)
                {
                    var intent = classifications[i];

                    // Only call subclassifier if greater than expected threshold
                    if (intent.Probability > threshold)
                    {
                        // Check for subcategory classifier
                        var subCatIntent = await InternalClassify(intent.Intent, message, threshold);
                        if (subCatIntent?.Length > 0)
                            results.AddRange(subCatIntent);
                    }

                    results.Add(intent);
                }

            }

            return results.ToArray();
        }

        async Task<ClassificationResponse[]> CheckForCommonChat(string message, bool allowSmsClassifications)
        {
            var classifierInfo = classifierConfigs["commonchat"];

            // Special case
            if (Regex.IsMatch(message, GreetingRegex, RegexOptions.IgnoreCase))
            {
                var intent = "commonchat-Greeting";
                return new ClassificationResponse[] { new ClassificationResponse
                {
                        Intent = intent,
                        Result = new Dictionary<string, object> {{ "intent", intent }, { "selected_system", "commonchat" }, {"score", 1.0 }},
                        Probability = 1.0,
                        Source = "luis"
                } };
            }

            var response = await CallLuisClassifier(classifierInfo, message);

            // Filter commonchat-Stop for non-SMS channel
            if (response == null || allowSmsClassifications)
                return response;

            foreach (var item in response.Where(x => x.Intent == "commonchat-Stop"))
            {
                item.Intent = "commonchat-None";
            }

            return response;
        }

        async Task<ClassificationResponse[]> ClassifyText(string subcategory, string message)
        {
            var classifierInfo = classifierConfigs[subcategory];
            if (classifierInfo == null)
                return null;

            if (classifierInfo.Method == "luis")
                return await CallLuisClassifier(classifierInfo, message);

            return await CallClassifier(classifierInfo, message);
        }

        async Task<ClassificationResponse[]> CallClassifier(ClassifierConfigSection info, string message)
        {
            var scoreRequest = new
            {
                Inputs = new Dictionary<string, StringTable>() { 
                    { 
                        "input1", 
                        new StringTable() 
                        {
                            ColumnNames = new string[] {"category", "text" },
                            Values = new string[,] {  { "value", message } }
                        }
                    },
                },
                GlobalParameters = new Dictionary<string, string>()
                {
                }
            };

            RestApiService restApiService = new RestApiService(info.Url, info.Key);
            string result = await restApiService.CallRestApi(null, JsonConvert.SerializeObject(scoreRequest), "application/json");

            if (result == null)
            {
                logger.ErrorFormat("Network: Failed to call classifier service. {0}", info.Url);

                return new ClassificationResponse[] { };
            }

            var answer = JsonConvert.DeserializeObject<dynamic>(result);

            Dictionary<string, object> results = ConvertTableToDictionary(answer.Results.output1.value);
            List<ClassificationResponse> classifications = new List<ClassificationResponse>();

            foreach (var key in results.Keys)
            {
                var match = Regex.Match(key, "Scored Probabilities for Class \"(.*)\"");
                if (match.Success)
                {
                    double probability = (double)results[key];
                    classifications.Add(new ClassificationResponse
                    {
                        Intent = match.Groups[1].Value,
                        Result = new Dictionary<string, object> { { "intent", match.Groups[1].Value }, { "selected_system", info.Id }, { "score", probability } },
                        Probability = probability,
                        Source = "azureml",
                        SourceData = info.Id,
                        RawResponse = result //answer
                    });
                }
            }

            return classifications.OrderByDescending(c => c.Probability).Take(2).ToArray();
        }

        async Task<ClassificationResponse[]> CallLuisClassifier(ClassifierConfigSection info, string message)
        {
            LuisService classifier = new LuisService(new UrlConfig() { Url = info.Url, Key = info.Key }, null);
            var luisResponse = await classifier.Parse(message, 0);

            if (luisResponse == null)
                return new ClassificationResponse[] { };

            // TODO: Need to figure out how to handle WrongIntent. See AVA-3345 for info.
            // Normally commonchat always overrides the flow.
            // But for WrongIntent, it doesn't always make sense, and we want the flow to use its error handler.

            return (from c in luisResponse.Intents
                    where c.Intent != "WrongIntent"
                    orderby c.Score descending
                    select new ClassificationResponse
                    {
                        Intent = $"{info.Id}-{c.Intent}",
                        Result = new Dictionary<string, object> {{ "intent", $"{info.Id}-{c.Intent}" }, { "selected_system", info.Id }, {"score", c.Score }},
                        Probability = c.Score,
                        Source = "luis",
                        RawResponse = JsonConvert.SerializeObject(luisResponse)
                    })
                    .Take(1).ToArray();
        }

        private static Dictionary<string, object> ConvertTableToDictionary(dynamic value)
        {
            var results = new Dictionary<string, object>();

            JArray names = value.ColumnNames;
            JArray types = value.ColumnTypes;
            JArray values = value.Values[0];

            for (int i = 0; i < names.Count; i++)
            {
                var n = (string)names[i];
                var type = (string)types[i];
                if (type == "String")
                {
                    string v = (string)values[i];
                    results.Add(n, v);
                }
                else if (type == "Double")
                {
                    double v = (double)values[i];
                    results.Add(n, v);
                }
            }
            return results;
        }

        public static string[] Tokenize(string text)
        {
            var tokens = text.Split(simpleTokenSplitter);

            return (from t in tokens
                    where !String.IsNullOrEmpty(t)
                    select t).ToArray();
        }

        static string Hash(string input)
        {
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder(hash.Length * 2);

            foreach (byte b in hash)
            {
                sb.Append(b.ToString("X2"));
            }

            return sb.ToString();
        }

    }
}
