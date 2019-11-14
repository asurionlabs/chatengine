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
using ChatWeb.Helpers;
using ChatWeb.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace ChatWeb.Services
{
    public class LuisService
    {
        const int MaxServiceRetries = 5;
        const int WarnSendTime = 15;
        const int MaxLuisTextLength = 450;
        const int WarnLuisMaxLength = 200;

#if XRAY2
        static readonly HttpClient client = AwsUtilityMethods.IsRunningOnAWS ? new HttpClient(new HttpClientXRayTracingHandler(new HttpClientHandler())) : new HttpClient();
#else
        static readonly HttpClient client = HttpClientFactory.Create(new[] { new XRayTracingMessageHandler() });
#endif

        readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        readonly string BaseUrl;
        readonly string SpellCheckerKey;

        public LuisService(UrlConfig urlConfig, string spellCheckerKey)
        {
            if (urlConfig == null)
                throw new ArgumentNullException(nameof(urlConfig));

            if (String.IsNullOrEmpty(urlConfig.Url))
                throw new ArgumentNullException(nameof(urlConfig) + ".Url");

            if (String.IsNullOrEmpty(urlConfig.Key))
                throw new ArgumentNullException(nameof(urlConfig) + ".Key");

            this.BaseUrl = urlConfig.Url;
            this.SpellCheckerKey = spellCheckerKey;

            lock (client)
            {
                if (!client.DefaultRequestHeaders.Contains("Ocp-Apim-Subscription-Key"))
                    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", urlConfig.Key);
            }
        }

        public async Task<LuisResponse> Parse(string text, int userTimeZoneOffset)
        {
            if (text.Length > WarnLuisMaxLength)
                logger.WarnFormat("Parse: Detected long text that may not be properly parsed for entities by Luis");

            // Specifying verbose gives all the intents
            var queryString = BuildQueryString(text, userTimeZoneOffset, SpellCheckerKey);
            string url = BaseUrl + "?" + queryString;
            logger.DebugFormat("Network: Luis calling {0}", url);

            int tries = 0;
            bool retryable = true;

            do
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                HttpResponseMessage response = await client.GetAsync(url).ConfigureAwait(false);

                LogLongResponseTime(sw, response);

                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();

                    logger.DebugFormat("Network: Luis received {0}", result);
                    return JsonConvert.DeserializeObject<LuisResponse>(result);
                }

                logger.ErrorFormat("Network: Failed to call luis classifier service. Attempt {0}/{1}. Status Code: {2}. Url: {3}", tries, MaxServiceRetries, response.StatusCode, BaseUrl);

                retryable = (((int)response.StatusCode < 400) || ((int)response.StatusCode >= 500));

            } while (retryable || (tries++ < MaxServiceRetries));

            return null;
        }

        private static NameValueCollection BuildQueryString(string text, int userTimeZoneOffset, string spellCheckerKey)
        {
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            if (!String.IsNullOrEmpty(spellCheckerKey))
            {
                queryString["bing-spell-check-subscription-key"] = spellCheckerKey;
                queryString["spellCheck"] = "true";
            }

            queryString["verbose"] = "true";

            if (userTimeZoneOffset != 0)
            {
                // Make sure timezone is in proper format.   ex: 0800
                if ((userTimeZoneOffset > -100) && (userTimeZoneOffset < 100))
                    userTimeZoneOffset *= 100;

                queryString["timezoneOffset"] = userTimeZoneOffset.ToString();
            }

            // Workaround LUIS bugs.
            // 1. A phrase with 2 periods will fail to detect a date.  Ex:  "yesterday.."
            // 2. Text longer than 500, will cause LUIS to return 414 (URL too long) even for POST
            queryString["q"] = text.Substring(0, Math.Min(text.Length, MaxLuisTextLength)).Replace("..", ".");

            return queryString;
        }

        private void LogLongResponseTime(Stopwatch sw, HttpResponseMessage response)
        {
            var elapsedTime = sw.Elapsed.TotalSeconds;
            if (elapsedTime > WarnSendTime)
                logger.WarnFormat("Network: LuisService took {0}. Status Code: {1}. Url: {2}", elapsedTime, response.StatusCode, BaseUrl);
            sw.Stop();
        }
    }
}
