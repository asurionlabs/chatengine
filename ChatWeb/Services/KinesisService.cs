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

using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using Amazon.Runtime;
using Amazon.Util;
using Amazon.XRay.Recorder.Core;
using ChatWeb.Helpers;
//using Amazon.XRay.Recorder.Handlers.AwsSdk;
using ChatWeb.Models;
using log4net.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatWeb.Services
{
    public class KinesisService : IDisposable
    {
        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        readonly string StreamName;
        readonly AmazonKinesisClient kinesisClient;
        readonly UserDataFilterService filterService;
        bool disposed;

        static readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        };


        public KinesisService(KinesisConfig kinesisConfig, UserDataFilterService filterService)
        {
            this.StreamName = kinesisConfig?.StreamName ?? throw new ArgumentNullException(nameof(kinesisConfig));
            this.filterService = filterService;

            if (!String.IsNullOrEmpty(kinesisConfig.Arn))
            {
                AssumeRoleAWSCredentials credentials = new AssumeRoleAWSCredentials(FallbackCredentialsFactory.GetCredentials(), kinesisConfig.Arn, kinesisConfig.StsRoleSessionName, new AssumeRoleAWSCredentialsOptions
                {
                    DurationSeconds = ChatConfiguration.StsCredentialExpiration
                });
                kinesisClient = new AmazonKinesisClient(credentials);
            }
            else
            {
                kinesisClient = new AmazonKinesisClient();
            }
#if !XRAY2
            string whitelistPath = System.Web.Hosting.HostingEnvironment.MapPath("/AWSWhitelist.json");

            var tracer = new Amazon.XRay.Recorder.Handlers.AwsSdk.AWSSdkTracingHandler(AWSXRayRecorder.Instance, whitelistPath);
            tracer.AddEventHandler(kinesis);
#endif
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources.
                    if (kinesisClient != null)
                        kinesisClient.Dispose();
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }
            disposed = true;
        }

        public async Task SendFailToUnderstandEvent(ChatModel chat, ChatFlowStep flowStep)
        {
            if (chat == null)
                return;

            var lastMessage = chat.CurrentState.GetLastMessage();

            var properties = new Dictionary<string, object>();
            AddStandardProperties(chat.CurrentState.SessionData, properties);

            KinesisLogEvent logEvent = new KinesisLogEvent
            {
                Session_id = chat.SessionLogId,
                Logger_info = String.Format("flow_engine:{0}:{1}", flowStep.Flow, flowStep.Id),
                Application_info = chat.CurrentState.SessionData.PartnerContext,
                Data = "",
                Log = "",
                To_index = true,
                Index_type = "engine_flowfailure",
                Index_name = "flow_failures",
                Index_content = new
                {
                    chat_id = chat.ChatId,
                    session_id = chat.SessionLogId,
                    // Can't use standard "o" format, since it provides 6 digit milliseconds, which causes errors when its saved to Dynamo
                    timestamp = AWSDynamoService.ToDynamoDateString(DateTime.UtcNow),
                    step = new ChatStepId(flowStep),
                    chat_text = filterService.FilterUserData(chat.CurrentState, lastMessage.UserInput),
                    chat_corrected_text = filterService.FilterUserData(chat.CurrentState, lastMessage.CorrectedUserInput),
                    bot_text = filterService.FilterUserData(chat.CurrentState, lastMessage.BotQuestionsText),
                    properties
                }
            };

            await PutRecordInternalAsync(logEvent);
        }

        public void SendTagEvent(SessionData sessionData, string tagName, IDictionary<string, object> properties)
        {
            KinesisLogEvent logEvent = CreateTagEvent(sessionData, tagName, properties);

            PutRecordInternal(logEvent);
        }

        public async Task SendTagEventAsync(SessionData sessionData, string tagName, IDictionary<string, object> properties)
        {
            KinesisLogEvent logEvent = CreateTagEvent(sessionData, tagName, properties);

            await PutRecordInternalAsync(logEvent);
        }

        private static KinesisLogEvent CreateTagEvent(SessionData sessionData, string tag, IDictionary<string, object> properties)
        {
            if (properties == null)
                properties = new Dictionary<string, object>();

            AddStandardProperties(sessionData, properties);

            var indexName = (properties != null && properties.ContainsKey("partner")) ? "tag_events_" + ((string)properties["partner"]).ToLower() : "tag_events";
            var indexType = (properties != null && properties.ContainsKey("partner")) ? "tag_events_" + ((string)properties["partner"]).ToLower() : "claims_tag_events";

            KinesisLogEvent logEvent = new KinesisLogEvent
            {
                Session_id = sessionData.SessionId,
                Logger_info = "flow_engine:tag_events",
                Application_info = sessionData.PartnerContext,
                Data = "",
                Log = "",
                To_index = true,
                Index_type = indexType,
                Index_name = indexName,
                Index_content = new
                {
                    chat_id = sessionData.ChatId,
                    session_id = sessionData.SessionId,
                    // Can't use standard "o" format, since it provides 6 digit milliseconds, which causes errors when its saved to Dynamo
                    timestamp = AWSDynamoService.ToDynamoDateString(DateTime.UtcNow),
                    tag,
                    properties
                }
            };
            return logEvent;
        }

        private static void AddStandardProperties(SessionData sessionData, IDictionary<string, object> properties)
        {
            properties["partner"] = sessionData.Partner?.ToLower();
            properties["context"] = sessionData.Context?.ToLower();
            properties["channel"] = sessionData.Channel;
            properties["distinct_id"] = sessionData.DistinctId;
            properties["utm_campaign"] = sessionData.UtmCampaign;
            properties["utm_medium"] = sessionData.UtmMedium;
            properties["utm_source"] = sessionData.UtmSource;
        }

        public async Task SendTranscriptEvent(ChatModel chat, string userInput, Stopwatch sw, ChatStepId[] currentSteps)
        {
            if (String.IsNullOrEmpty(StreamName))
                return;

            var lastStep = currentSteps.LastOrDefault();
            if (lastStep == null)
                lastStep = new ChatStepId("none", "0");

            var data = new KinesisTranscriptData()
            {
                ProcessTime = sw.ElapsedMilliseconds,
                DetectedIntent = chat?.CurrentState.LastClassification?.GetBestResult()?.Intent,
                StepsTaken = currentSteps,
                Channel = chat?.CurrentState?.GlobalAnswers.GetFieldAnswer(ChatStandardField.Channel)
            };

            if (chat?.CurrentState?.LastClassification?.GetBestResult() != null)
                data.Probability = chat.CurrentState.LastClassification.GetBestResult().Probability;

            KinesisLogEvent logEvent = new KinesisLogEvent
            {
                Session_id = chat?.ChatId ?? "00000000000000000000000000000000",
                Logger_info = String.Format("flow_engine:{0}:{1}", lastStep.FlowName, lastStep.StepId),
                Application_info = chat?.CurrentState.SessionData.PartnerContext,
                Data = JsonConvert.SerializeObject(data),
                Log = JsonConvert.SerializeObject(new KinesisLogMessage()
                {
                    User = filterService.FilterHighRiskPII(chat?.CurrentState, userInput),
                    Ava = chat?.CurrentState.GetLastMessage().BotQuestionsText
                }
                )
            };

            await PutRecordInternalAsync(logEvent);
        }

        public async Task SendClassificationEvent(ChatModel chat, Chat_ParseField field, bool success, bool searchMode, string source, LuisResponse luis)
        {
            var content = CreateParseEventContent(chat, field, success, searchMode);
            content.Result = luis.TopScoringIntent?.Intent ?? "None";
            content.ResultData = new Dictionary<string, object> { { "intent", content.Result }, { "selected_system", source }, { "score", luis.TopScoringIntent?.Score ?? 0 } };
            content.ResultProbability = luis.TopScoringIntent?.Score ?? 0;
            content.Source = "luis";
            content.Classifications = new KinesisClassification[] {
                    new KinesisClassification {
                        Model_source = "luis",
                        Model_prediction = luis.GetTopIntent(0),
                        Model_score = luis.TopScoringIntent?.Score ?? 0,
                        Model_rawResponse = JsonConvert.SerializeObject(luis)
                    }
                };

            await InternalSendKinesisParseEvent("classification", "intent_classifier", chat, content);
        }

        public async Task SendClassificationEvent(ChatModel chat, Chat_ParseField field, bool success, bool searchMode, ClassificationResults classification)
        {
            var content = CreateParseEventContent(chat, field, success, searchMode);

            var bestResult = classification.GetBestResult();

            if (bestResult != null)
            {
                content.Source = bestResult.Source;
                content.Result = bestResult.Intent;
                content.ResultData = bestResult.Result;
                content.ResultProbability = bestResult.Probability;
            }

            content.Classifications = (from c in classification.AllResults
                                       select new KinesisClassification
                                       {
                                           Model_source = c.Source,
                                           Model_prediction = c.Intent,
                                           Model_score = c.Probability,
                                           Model_rawResponse = c.RawResponse
                                       }).ToArray();
                                       
            await InternalSendKinesisParseEvent("classification", "intent_classifier", chat, content);
        }

        public async Task SendParseEvent(ChatModel chat, Chat_ParseField field, bool success, bool searchMode, string answer)
        {
            var content = CreateParseEventContent(chat, field, success, searchMode);

            var filteredAnswer = filterService.FilterUserData(chat.CurrentState, answer);

            content.Parse = new
            {
                rule = field.RuleData,
                answer = filteredAnswer
            };
            content.Result = filteredAnswer;
            content.ResultProbability = success ? 1.0 : 0.0;


            string indexType = "intent_unknown";
            switch (field.ParseType)
            {
                case ChatRuleType.AddressParser: indexType = "intent_address"; break;
                case ChatRuleType.AppNameParser: indexType = "intent_appname"; break;
                case ChatRuleType.BluetoothDeviceParser: indexType = "intent_bluetoothdevice"; break;
                case ChatRuleType.ColorParser: indexType = "intent_color"; break;
                case ChatRuleType.ContinueParser: indexType = "intent_continue"; break;
                case ChatRuleType.DontKnowParser: indexType = "intent_dontknow"; break;
                case ChatRuleType.DateParser:
                case ChatRuleType.DateParserV2: indexType = "intent_date"; break;
                case ChatRuleType.Email: indexType = "intent_email"; break;
                case ChatRuleType.FuzzyMatch: indexType = "intent_fuzzymatch"; break;
                case ChatRuleType.IntentGatewayParser: indexType = "intent_intentgateway"; break;
                case ChatRuleType.LossCategoryParserOptions:
                case ChatRuleType.LossCategoryParser: indexType = "intent_losscategory"; break;
                case ChatRuleType.NameParse:
                case ChatRuleType.NameParseNoHistory: indexType = "intent_name"; break;
                case ChatRuleType.NumberParser: indexType = "intent_number"; break;
                case ChatRuleType.Parse: indexType = "intent"; break;
                case ChatRuleType.ParseBackupProvider: indexType = "intent_backupprovider"; break;
                case ChatRuleType.ParseCarrier: indexType = "intent_carrier"; break;
                case ChatRuleType.ParseDevice: indexType = "intent_device"; break;
                case ChatRuleType.ParseRetailPartner: indexType = "intent_retailpartner"; break;
                case ChatRuleType.PauseParser: indexType = "intent_pause"; break;
                case ChatRuleType.PhoneNumber:
                case ChatRuleType.PhoneNumberNoHistory: indexType = "intent_phone"; break;
                case ChatRuleType.Regex: indexType = "intent_regex"; break;
                case ChatRuleType.TimeParser: indexType = "intent_time"; break;
                case ChatRuleType.YesNoParser: indexType = "intent_yesno"; break;
                case ChatRuleType.ZipCodeParser: indexType = "intent_zipcode"; break;
            }

            await InternalSendKinesisParseEvent(field.ParseType, indexType, chat, content);
        }

        KinesisParseEventContent CreateParseEventContent(ChatModel chatModel, Chat_ParseField field, bool success, bool searchMode)
        {
            var step = chatModel.CurrentState.Steps.LastOrDefault();
            var lastMessage = chatModel.CurrentState.GetLastMessage();
            var properties = new Dictionary<string, object>();
            var interactionId = log4net.LogicalThreadContext.Properties["interactionId"] as string;
            AddStandardProperties(chatModel.CurrentState.SessionData, properties);

            return new KinesisParseEventContent()
            {
                Chat_id = chatModel.ChatId,
                Interaction_id = interactionId,
                Session_id = chatModel.SessionLogId,
                Session_date = DateTime.UtcNow.ToString("o"),
                Variable_name = field?.FieldName,
                FlowStep = new ChatStepId(step),
                Chat_text = filterService.FilterUserData(chatModel.CurrentState, lastMessage.UserInput),
                Chat_corrected_text = filterService.FilterUserData(chatModel.CurrentState, lastMessage.CorrectedUserInput),
                Bot_text = filterService.FilterUserData(chatModel.CurrentState, lastMessage.BotQuestionsText),
                Success = success,
                SearchMode = searchMode,
                Properties = properties
            };
        }

        async Task InternalSendKinesisParseEvent(string parseType, string indexType, ChatModel chat, dynamic content)
        {
            var indexName = "classifier_data";
            if (!String.IsNullOrEmpty(chat.CurrentState.SessionData.Partner))
                indexName = String.Format("classifier_data_{0}_{1}", chat.CurrentState.SessionData.Context, chat.CurrentState.SessionData.Partner).ToLower();

            KinesisLogEvent logEvent = new KinesisLogEvent
            {
                Session_id = chat.ChatId ?? "00000000000000000000000000000000",
                Logger_info = $"flow_engine:{parseType}",
                Application_info = chat.CurrentState.SessionData.PartnerContext,
                Data = "",
                Log = "",
                To_index = true,
                Index_type = indexType,
                Index_name = indexName,
                Index_content = content
            };

            await PutRecordInternalAsync(logEvent);
        }

        /// <summary>
        /// Sends a record to the specific AWS Kinesis stream.
        /// NOTE: The message object is not checked for PII data, so the caller must check for PII requirements if necessary.
        /// </summary>
        /// <param name="kinesisConfig">Kinesis stream information</param>
        /// <param name="message"></param>
        public static void PutRecord(KinesisConfig kinesisConfig, object message)
        {
            using (var kinesisService = new KinesisService(kinesisConfig, null))
            {
                kinesisService.PutRecordInternal(message);
            }
        }

        void PutRecordInternal(object message)
        {
            try
            {
                byte[] oByte = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message, serializerSettings));

                using (MemoryStream stream = new MemoryStream(oByte))
                {
                    PutRecordRequest request = new PutRecordRequest()
                    {
                        PartitionKey = Guid.NewGuid().ToString("N"),
                        StreamName = StreamName,
                        Data = stream
                    };

                    kinesisClient.PutRecord(request);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Network: Exception sending log to kinesis.", ex);
            }
        }

        async Task PutRecordInternalAsync(object message)
        {
            try
            {
                byte[] oByte = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message, serializerSettings));

                using (MemoryStream stream = new MemoryStream(oByte))
                {
                    PutRecordRequest request = new PutRecordRequest()
                    {
                        PartitionKey = Guid.NewGuid().ToString("N"),
                        StreamName = StreamName,
                        Data = stream
                    };

                    await kinesisClient.PutRecordAsync(request);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Network: Exception sending log to kinesis.", ex);
            }
        }
    }
}