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

using ChatWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ChatWeb.Helpers;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using System.Dynamic;
using Microsoft.ClearScript;
using System.Net.Mail;
using Amazon.XRay.Recorder.Core;
using System.IO;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace ChatWeb.Services
{
    public class ChatScriptHost : IDisposable
    {
        const int DefaultServiceRetries = 1;
        const int MaxDnsAttempts = 2;

        const int DefaultTimeout = 60;
        const int MaxTimeout = 120000;  // 2 minutes
        const string AppleRegex = "apple|iphone|ipad|ios";

        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        readonly UserDataFilterService userDataFilterService = new UserDataFilterService();
        readonly ChatModel chatModel;
        Dictionary<string, object> Properties = new Dictionary<string, object>();
        LambdaService lambdaService = new LambdaService();
        readonly ChatConfiguration Configuration;
         
        public ChatScriptHost(ChatModel chatModel, ChatConfiguration configuration)
        {
            this.chatModel = chatModel;
            this.Configuration = configuration;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (lambdaService != null)
                    {
                        lambdaService.Dispose();
                        lambdaService = null;
                    }
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion

        public bool DummyMode { get; set; }
        public ScriptEngine ScriptContext { get; set; }

        public void ClearProperties()
        {
            Properties.Clear();
        }

        public bool ContainsProperty(string propertyName)
        {
            return Properties.ContainsKey(propertyName);
        }

        public object GetProperty(string propertyName)
        {
            if (!Properties.ContainsKey(propertyName))
                return null;

            return Properties[propertyName];
        }

        public T GetProperty<T>(string propertyName)
        {
            object value = GetProperty(propertyName);

            if (value == null)
                return default(T);

            return (T)Convert.ChangeType(value, typeof(T));
        }

        void SetProperty(string propertyName, object value)
        {
            Properties[propertyName] = value;
        }

        public void AddToPropertyList(string propertyName, object value)
        {
            if (!Properties.ContainsKey(propertyName))
                Properties[propertyName] = new List<object>();

            List<object> list = (List<object>)Properties[propertyName];
            list.Add(value);
        }

        public string GetDeviceOS(object selectedDevice)
        {
            if (!(selectedDevice is string selectedDeviceText))
                throw new ApplicationException($"{nameof(GetDeviceOS)}: {nameof(selectedDevice)} must be a string");

            if (!String.IsNullOrEmpty(selectedDeviceText))
                return DetectDeviceOS(selectedDeviceText);

            return null;
        }

        private static string DetectDeviceOS(object device)
        {
            if (!(device is string deviceText))
                throw new ApplicationException($"{nameof(DetectDeviceOS)}: {nameof(device)} must be a string");

            if (Regex.IsMatch(deviceText, AppleRegex, RegexOptions.IgnoreCase))
                return "iOS";

            return "Android";
        }

        public void AddPiiText(int piiType, object text, object mask)
        {
            if (!(text is string textText))
                throw new ApplicationException($"{nameof(AddPiiText)}: {nameof(text)} must be a string");

            if (!(mask is string maskText))
                throw new ApplicationException($"{nameof(AddPiiText)}: {nameof(mask)} must be a string");

            if (String.IsNullOrEmpty(textText))
                return;

            PIIType pii = (PIIType)piiType;
            chatModel.CurrentState.AddPIIText(pii, textText, maskText);
        }

        public double GetCurrentTimeZoneOffset(object targetTimezone)
        {
            if (!(targetTimezone is string targetTimezoneText))
                throw new ApplicationException($"{nameof(GetCurrentTimeZoneOffset)}: {nameof(targetTimezone)} must be a string");

            TimeZoneInfo timeInfo = TimeZoneInfo.FindSystemTimeZoneById(targetTimezoneText);
            var offset = timeInfo.GetUtcOffset(DateTime.UtcNow);
            return offset.TotalHours;
        }

        public object CallLambda(object function, dynamic options, dynamic values)
        {
            if (!(function is string functionText))
                throw new ApplicationException($"{nameof(CallLambda)}: {nameof(function)} must be a string");

            LambdaRequestOptions lambdaOptions = ProcessLambdaRequestOptions(options);

            string data = null;
            var native = ConvertToNative(values);
            if (native != null)
                data = JsonConvert.SerializeObject(native);

            var response = lambdaService.Invoke(functionText, data);

            if (lambdaOptions.KeepResponseAsString)
                return response;

            var result = JsonConvert.DeserializeObject<dynamic>(response);
            return ScriptHelpers.ToScriptValue(result, ScriptContext);
        }

        public ExpandoObject MakeWebRequest(object apiUrl, object apiMethod, dynamic values)
        {
            if (!(apiUrl is string apiUrlText))
                throw new ApplicationException($"{nameof(MakeWebRequest)}: {nameof(apiUrl)} must be a string");
            if (!(apiMethod is string apiMethodText))
                throw new ApplicationException($"{nameof(MakeWebRequest)}: {nameof(apiMethod)} must be a string");

            return MakeWebRequestEx(apiUrl, apiMethod, null, values);
        }

        public object MakeWebRequestEx(object apiUrl, object apiMethod, dynamic options, dynamic values)
        {
            if (!(apiUrl is string apiUrlText))
                throw new ApplicationException($"{nameof(MakeWebRequestEx)}: {nameof(apiUrl)} must be a string");
            if (!(apiMethod is string apiMethodText))
                throw new ApplicationException($"{nameof(MakeWebRequestEx)}: {nameof(apiMethod)} must be a string");

            int tries = 0;
            int dnsAttempts = 1;
            WebRequestOptions webRequestOptions = ProcessWebRequestOptions(options);

            do
            {
                logger.InfoFormat("Script: Calling web url: {0}", apiUrlText);
                Uri uri = new Uri(apiUrlText);  // Makes sure the url is valid

                try
                {
                    // Using older WebClient class since it has non-async methods.
                    // We can't use Async calls here because we aren't passing the async/await context through javscript.
                    // That requires using the javascript callback method and makes the javascript more complex.
                    using (WebClientEx wc = new WebClientEx(webRequestOptions.Timeout))
                    {
                        // fix for possible performance issue with WebClient class
                        // https://stackoverflow.com/questions/4932541/c-sharp-webclient-acting-slow-the-first-time
                        wc.Proxy = null;

                        Stopwatch sw = new Stopwatch();
                        sw.Start();

                        if (DummyMode)
                            wc.Headers.Add("X-DummyMode", "True");

                        // Add headers
                        if (webRequestOptions.Headers != null)
                        {
                            foreach (var header in webRequestOptions.Headers)
                            {
                                wc.Headers.Add(header.Key, header.Value?.ToString());
                            }
                        }

                        string response;

                        if (apiMethodText.Equals("GET", StringComparison.OrdinalIgnoreCase))
                        {
                            response = wc.DownloadString(uri);
                        }
                        else
                        {
                            string data = null;
                            if (webRequestOptions.KeepRequestAsString)
                                data = values;
                            else
                            {
                                var native = ConvertToNative(values);
                                if (native != null)
                                    data = JsonConvert.SerializeObject(native);
                            }

                            logger.DebugFormat("Script: Request: {0}", data);
                            response = wc.UploadString(uri, apiMethodText, data);
                        }

                        logger.DebugFormat("Script: Response: {0}", response);

                        sw.Stop();
                        var elapsedTime = sw.Elapsed.TotalSeconds;
                        if ((ChatConfiguration.DefaultRestAPIWarnSendTime >= 0) && (elapsedTime > ChatConfiguration.DefaultRestAPIWarnSendTime))
                            logger.WarnFormat("Network: MakeWebRequestEx() took {0} seconds. Url: {1}", elapsedTime, uri);

                        if (webRequestOptions.KeepResponseAsString)
                            return response;

                        var result = JsonConvert.DeserializeObject<dynamic>(response);
                        return ScriptHelpers.ToScriptValue(result, ScriptContext);
                    }
                }
                catch (WebException ex)
                {
                    if (ex.Status == WebExceptionStatus.NameResolutionFailure)
                    {
                        // WebClient gets DNS failures sometimes, so we auto retry.
                        if (++dnsAttempts <= MaxDnsAttempts)
                        {
                            logger.WarnFormat("Script: Warning DNS failure. Retrying: {0}", apiUrl);

                            // dont count as regulare try
                            tries--;
                            continue;
                        }

                        logger.ErrorFormat("Script: Error calling url {0}. {1}, {2}", apiUrl, ex.Status, ex.ToString());
                        return null;
                    }

                    logger.ErrorFormat("Script: Error calling url {0}. {1}, {2}", apiUrl, ex.Status, ex.ToString());

                    if (ex.Status == WebExceptionStatus.ProtocolError)
                    {
                        var webResponse = (HttpWebResponse)ex.Response;

                        var errorBody = new StreamReader(webResponse.GetResponseStream()).ReadToEnd();
                        logger.DebugFormat("Script: Error Response: {0}", errorBody);

                        if (((int)webResponse.StatusCode >= 400) && ((int)webResponse.StatusCode <= 499))
                        {
                            // log 400 error. not re-try-able);
                            break;
                        }
                    }
                }
            } while (tries++ < webRequestOptions.Retries);

            return null;
        }

        private static LambdaRequestOptions ProcessLambdaRequestOptions(dynamic options)
        {
            var lambdaOptions = new LambdaRequestOptions();

            if (options == null)
                return lambdaOptions;

            var o = (object)options;
            IDictionary<string, object> convertedValue = o.FromScriptValue() as IDictionary<string, object>;

            if (convertedValue.ContainsKey("KeepResponseAsString"))
                lambdaOptions.KeepResponseAsString = Convert.ToBoolean(convertedValue["KeepResponseAsString"]);

            return lambdaOptions;
        }

        private static WebRequestOptions ProcessWebRequestOptions(dynamic options)
        {
            var webRequestOptions = new WebRequestOptions()
            {
                Retries = DefaultServiceRetries,
                Timeout = DefaultTimeout * 1000
            };

            // Decode options
            if (options != null)
            {
                var o = (object)options;
                IDictionary<string, object> convertedValue = o.FromScriptValue() as IDictionary<string, object>;

                if (convertedValue.ContainsKey("Retries"))
                    webRequestOptions.Retries = (int)convertedValue["Retries"];

                if (convertedValue.ContainsKey("Timeout"))
                    webRequestOptions.Timeout = (int)convertedValue["Timeout"] * 1000;

                if (convertedValue.ContainsKey("Headers"))
                    webRequestOptions.Headers = convertedValue["Headers"] as Dictionary<string, object>;

                if (convertedValue.ContainsKey("KeepResponseAsString"))
                    webRequestOptions.KeepResponseAsString = Convert.ToBoolean(convertedValue["KeepResponseAsString"]);

                if (convertedValue.ContainsKey("KeepRequestAsString"))
                    webRequestOptions.KeepRequestAsString = Convert.ToBoolean(convertedValue["KeepRequestAsString"]);
            }

            if (webRequestOptions.Timeout > MaxTimeout)
            {
                logger.WarnFormat("Script requesting timeout of {0} which is greater than max allowed value of {1}", webRequestOptions.Timeout, MaxTimeout);
                webRequestOptions.Timeout = MaxTimeout;
            }

            return webRequestOptions;
        }

        public void Abort(string message = null)
        {
            if (String.IsNullOrEmpty(message))
                message = "Unspecified error";

            chatModel.CurrentState.GetFlowAnswers("BadFlow")["errMessage"] = message;
            SetProperty("ChildStepId", new ChatStepId("BadFlow", "9999998"));

            logger.ErrorFormat("Flow called api.Abort(). {0}", message);
        }

        public string CreateUniqueId()
        {
            return Guid.NewGuid().ToString("N");
        }

        public void SetNextStep(object stepId)
        {
            if (!(stepId is string stepIdText))
                throw new ApplicationException($"{nameof(SetNextStep)}: {nameof(stepId)} must be a string");

            SetChildFlow(null, stepIdText);
        }

        public void SetChildFlow(object flowName, object stepId)
        {
            if (!(flowName is string flowNameText))
                throw new ApplicationException($"{nameof(SetChildFlow)}: {nameof(flowName)} must be a string");
            if (!(stepId is string stepIdText))
                throw new ApplicationException($"{nameof(SetChildFlow)}: {nameof(stepId)} must be a string");

            SetProperty("ChildStepId", new ChatStepId(flowNameText, stepIdText));
        }

        public FlowResult CallFlow(object flowName, dynamic values, string partner = null, string context = null)
        {
            if (!(flowName is string flowNameText))
                throw new ApplicationException($"{nameof(CallFlow)}: {nameof(flowName)} must be a string");

            // Always go to start of called flow
            var stepId = "1";

            chatModel.CurrentState.GetFlowAnswers(flowNameText)["params"] = ConvertToNative(values);
            chatModel.CurrentState.GetFlowAnswers(flowNameText)["result"] = null;

            var resultPlaceholder = new FlowResult();
            SetProperty("ChildResultPlaceholder", resultPlaceholder);

            SetChildFlow(flowName, stepId);

            // TODO: Need to track state, to jump back partner/context
            if (!String.IsNullOrEmpty(partner) && !String.IsNullOrEmpty(context))
                ChangeContext(partner, context);


            return resultPlaceholder;
        }

        public void TransferToAgent(dynamic options, string skill = null)
        {
            logger.Info("Script: Transfer to Agent");

            SetProperty("TransferToAgent", true);
            SetProperty("TransferToAgentSkill", skill);
            SetProperty("TransferInfo", ((object)options).FromScriptValue());
        }

        public bool ChangeContext(object partner, object context)
        {
            if (!(partner is string partnerText))
                throw new ApplicationException($"{nameof(ChangeContext)}: {nameof(partner)} must be a string");
            if (!(context is string contextText))
                throw new ApplicationException($"{nameof(ChangeContext)}: {nameof(context)} must be a string");

            logger.InfoFormat("Script: Partner Context changed: {0}, {1}", partnerText, contextText);

            chatModel.CurrentState.ChangePartnerContext(partnerText, contextText);

            SendTagEvent("context_changed");

            return true;
        }

        public void IncreaseFailureCount()
        {
            SetProperty("IncreaseFailureCount", true);
        }

        public bool PutKinesisEvent(string arn, string streamName, string stsRoleSessionName, string partitionKey, dynamic eventData)
        {
            try
            {
                var data = (object)eventData;
                KinesisService.PutRecord(new KinesisConfig() { Arn = arn, StreamName = streamName, StsRoleSessionName = stsRoleSessionName }, data.FromScriptValue());
                return true;
            }
            catch (Exception ex)
            {
                logger.ErrorFormat("Error sending kinesis event. {0}", ex.Message);
            }

            return false;
        }

        public void SendTagEvent(object eventName, dynamic properties = null)
        {
            if (!(eventName is string eventNameText))
                throw new ApplicationException($"{nameof(SendTagEvent)}: {nameof(eventName)} must be a string");

            if (String.IsNullOrEmpty(eventNameText) || (ChatEngine.kinesisAnalyticsService == null))
                return;

            IDictionary<string, object> props;

            if (properties == null)
                props = new Dictionary<string, object>();
            else
                props = ((object)properties).FromScriptValue() as IDictionary<string, object>;

            ChatEngine.kinesisAnalyticsService.SendTagEvent(chatModel.CurrentState.SessionData, eventNameText.ToLower(), props);
        }

        public void UI(object customMarkup)
        {
            logger.Warn("Script: Flow still calling old version of api.UI() without specifying plain text.");
            UI(customMarkup, "");
        }

        public void UI(object customMarkup, object plainText)
        {
            if (!(customMarkup is string customMarkupText))
                throw new ApplicationException($"{nameof(UI)}: {nameof(customMarkup)} must be a string");
            if (!(plainText is string plainTextText))
                throw new ApplicationException($"{nameof(UI)}: {nameof(plainTextText)} must be a string");

            AddToPropertyList("UiMessages", new Tuple<string, string>(customMarkupText, plainTextText));
        }

        public void SetQuickReplies(dynamic choices)
        {
            try
            {
                if (choices != null)
                {
                    var objectType = (object)choices;
                    var convertedValue = objectType.FromScriptValue();
                    SetProperty("QuickReplyChoices", ConvertObjectToSpecificType<UserChoice[]>(convertedValue));
                }
            }
            catch (Exception ex)
            {
                logger.ErrorFormat("Script: SetQuickReplies error. {0}", ex.Message);
            }
        }

        public object GetChatTranscript()
        {
            return chatModel.CurrentState.GetCurrentTranscript().ToScriptValue(ScriptContext);
        }

        public void SendEmail(object from, object recipient, object subject, object body, bool bodyIsHtml)
        {
            if (!(from is string fromText))
                throw new ApplicationException($"{nameof(SendEmail)}: {nameof(from)} must be a string");
            if (!(recipient is string recipientText))
                throw new ApplicationException($"{nameof(SendEmail)}: {nameof(recipient)} must be a string");
            if (!(subject is string subjectText))
                throw new ApplicationException($"{nameof(SendEmail)}: {nameof(subject)} must be a string");
            if (!(body is string bodyText))
                throw new ApplicationException($"{nameof(SendEmail)}: {nameof(body)} must be a string");


            if (AwsUtilityMethods.IsRunningOnAWS)
                AWSXRayRecorder.Instance.BeginSubsegment("Script: SendEmail");

            var smtpConfig = Configuration.Smtp;

            if (String.IsNullOrEmpty(fromText))
                from = smtpConfig.From;

            try
            {
                using (SmtpClient client = new SmtpClient(smtpConfig.Server, smtpConfig.Port))
                {
                    using (var message = new MailMessage(fromText, recipientText, subjectText, bodyText))
                    {
                        message.IsBodyHtml = bodyIsHtml;
                        client.Send(message);
                    }
                }
            }
            catch (Exception ex)
            {
                if (AwsUtilityMethods.IsRunningOnAWS)
                    AWSXRayRecorder.Instance.AddException(ex);
                logger.ErrorFormat("Script: Send Email Error. '{0}'", ex.Message);
            }
            finally
            {
                if (AwsUtilityMethods.IsRunningOnAWS)
                    AWSXRayRecorder.Instance.EndSubsegment();
            }
        }

        public void SetTrustedClientResponse(object data)
        {
            if (data == null)
                SetProperty("TrustedClientResponse", null);

            if (!(data.FromScriptValue() is Dictionary<string, object> dictionary))
                throw new ApplicationException($"{nameof(SetTrustedClientResponse)}: {nameof(data)} must be null or an object");

            SetProperty("TrustedClientResponse", dictionary);
        }

        private static T ConvertObjectToSpecificType<T>(object convertedValue)
        {
            var serialize = JsonConvert.SerializeObject(convertedValue);
            return JsonConvert.DeserializeObject<T>(serialize);
        }

        private static object ConvertToNative(dynamic values)
        {
            if (values == null)
                return null;

            return ((object)values).FromScriptValue();
        }

        public void LogInfoMessage(object message)
        {
            if (!(message is string messageText))
                throw new ApplicationException($"{nameof(LogInfoMessage)}: {nameof(message)} must be a string");

            userDataFilterService.FilterUserData(chatModel?.CurrentState, messageText);
            logger.InfoFormat("Script: {0}", messageText);
        }

        private class LambdaRequestOptions
        {
            public bool KeepResponseAsString;
        }

        private class WebRequestOptions
        {
            public int Timeout;
            public int Retries;
            public Dictionary<string, object> Headers;
            public bool KeepResponseAsString;
            public bool KeepRequestAsString;
        }

    }
}
 