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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Text;
using ChatWeb.Extensions;
using ChatWeb.Helpers;
using ChatWeb.Models;
using System.Globalization;
using Newtonsoft.Json;
using ChatWeb.Parsers;
using Amazon.XRay.Recorder.Core;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.NodeServices;

using Microsoft.AspNetCore.SignalR;
using ChatEngine.Hubs;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;

namespace ChatWeb.Services
{
    public class ChatEngine
    {
        public static KinesisService kinesisAnalyticsService;

        readonly IExternalDataStorageService externalDataStorageService;
        readonly IHubContext<NodeJSHub> nodeJsHubContext;
        readonly INodeServices nodeServices;
        readonly TextClassificationService classificationService;
        readonly SpellcheckService spellCheckService;
        readonly ChatConfiguration chatConfiguration;
        readonly EmojiParser emojiParser;
        readonly UserDataFilterService userDataFilterService;
        readonly AddressParseService addressParseService;
        readonly FuzzyMatchService fuzzyMatchService;
        readonly TextParserService textParserService;
        readonly FlowStepProvider flowStepProvider;

        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static KinesisService kinesisService;
        static DeviceCatalog deviceCatalog = new DeviceCatalog();
        static ColorCatalog colorCatalog = new ColorCatalog();
        static MemCacheService chatMemCacheService;
        static LuisService luisDamageClassifier;
        static LuisService luisDateParserService;
        static bool staticsInitialized;
        static object parseServiceLock = new object();
        static string appVersion = VersionService.GetVersion();
        static readonly TextInfo textInfo = Thread.CurrentThread.CurrentCulture.TextInfo;
        IChatScriptManager scriptManager;
        bool userAskedWhy;
        static readonly char[] splitWaitTime = new char[] { '_' };

        public object TransferInfo { get; set; }

        public ChatEngine(IHostingEnvironment hostingEnvironment,
            ChatConfiguration chatConfiguration,
            SpellcheckService spellCheckService,
            IExternalDataStorageService externalDataStorage, 
            IHubContext<NodeJSHub> nodeJsHubContext, 
            INodeServices nodeServices,
            TextClassificationService textClassificationService,
            EmojiParser emojiParser,
            UserDataFilterService userDataFilterService,
            FuzzyMatchService fuzzyMatchService,
            AddressParseService addressParseService,
            TextParserService textParserService,
            FlowStepProvider flowStepProvider)
        {
            this.nodeJsHubContext = nodeJsHubContext;
            this.nodeServices = nodeServices;
            this.externalDataStorageService = externalDataStorage;
            this.chatConfiguration = chatConfiguration;
            this.spellCheckService = spellCheckService;
            this.classificationService = textClassificationService;
            this.emojiParser = emojiParser;
            this.userDataFilterService = userDataFilterService;
            this.fuzzyMatchService = fuzzyMatchService;
            this.addressParseService = addressParseService;
            this.textParserService = textParserService;
            this.flowStepProvider = flowStepProvider;

            if (!staticsInitialized)
            {
                lock (parseServiceLock)
                {
                    // Check again after getting lock
                    if (!staticsInitialized)
                    {
                        InitStatics(hostingEnvironment.ContentRootPath);

                        staticsInitialized = true;
                    }
                }
            }
        }

        protected void InitStatics(string modelPath)
        {
            IncreaseRegexCacheSize();

            deviceCatalog.LoadDeviceListFromPath(modelPath);
            colorCatalog.LoadCatalog(Path.Combine(modelPath, "colorlist.csv"));

            // Note we only use spellchecker in luis for dates, to avoid any PII leakage sent to 3rd party service
            luisDamageClassifier = new LuisService(chatConfiguration.LuisDamageClassifier, null);
            luisDateParserService = new LuisService(chatConfiguration.LuisDamageClassifier, chatConfiguration.LuisSpellCheckerKey);
            chatMemCacheService = new MemCacheService(chatConfiguration.MemCacheConfigNode);

            if (!String.IsNullOrEmpty(chatConfiguration.KinesisLog?.StreamName))
                kinesisService = new KinesisService(chatConfiguration.KinesisLog, userDataFilterService);

            if (!String.IsNullOrEmpty(chatConfiguration.KinesisAnalytics?.StreamName))
                kinesisAnalyticsService = new KinesisService(chatConfiguration.KinesisAnalytics, userDataFilterService);
        }

        private static void IncreaseRegexCacheSize()
        {
            Regex.CacheSize = ChatConfiguration.RegexCacheSize;
        }

        public async Task<ChatResponse> HandleRequest(ChatRequest chatRequest)
        {
            ValidateInput(chatRequest);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            ChatModel chatModel = null;
            var chatMessage = new ChatMessage();

            try
            {
                chatRequest.UserInput = emojiParser.ReplaceEmoji(chatRequest.UserInput);
                chatRequest.UserInput = AnsiHelpers.StripNonAnsi(chatRequest.UserInput);

                bool isNewChat = false;

                chatModel = GetCurrentChat(chatRequest.ChatId);
                if (chatModel == null)
                {
                    chatModel = await CreateNewChat(chatRequest);
                    isNewChat = true;
                }

                if (chatRequest.DebugData != null)
                {
                    if (chatRequest.DebugData.SetVariables != null)
                        ProcessDebugVariables(chatModel.CurrentState, chatRequest.DebugData.SetVariables);

                    chatModel.UseDebugFlows = chatRequest.DebugData.UseDebugFlow;
                    chatModel.DebugUserId = chatRequest.DebugData.UserId;
                    if (!String.IsNullOrEmpty(chatModel.DebugUserId))
                    {
                        chatModel.UseNodeScripts = true;
                        chatModel.CurrentState.GlobalAnswers[ChatStandardField.IsNodeScript] = true;
                        if (chatModel.DebugUserId == "server")
                            chatModel.DebugUserId = null;
                    }
                }

                HandleChatParams(chatRequest, chatModel);
                SetLoggingProperties(chatModel, chatMessage);

                if (isNewChat)
                {
                    logger.Info("Internal: Session Start");

                    if (chatConfiguration.ShowDebugChatMessages)
                    {
                        chatMessage.AddUiText(ChatObjectType.StaticMessage,
                            $"<a target=\"_blank\" href=\"{chatConfiguration.KibanaLogUrl}/app/kibana#/discover?_g=(time:(from:now-24h,mode:quick,to:now))&_a=(columns:!(severity,serviceName,message),index:'log_events*',interval:auto,query:(query_string:(analyze_wildcard:!t,query:'session_id:{chatModel.SessionLogId}')),sort:!(timestamp,desc))\">Session Log</a>",
                            null);
                    }
                }

                if (AwsUtilityMethods.IsRunningOnAWS)
                    AWSXRayRecorder.Instance.AddAnnotation("SessionId", chatModel.ChatId);

                logger.DebugFormat("Internal: request: TimeZone: {0}, DebugMode: {1}, TrustedClientData: {2}, Timeout: {3}, Input: {4}", chatRequest.TimeZone, chatRequest.DebugData != null, chatRequest.TrustedClientData != null, chatRequest.ChatTimeout, chatRequest.UserInput);

                var chatFlowStep = chatModel.CurrentState.Steps.LastOrDefault();
                var lastChatMessage = chatModel.CurrentState.GetLastMessage();
                lastChatMessage.UserInput = chatRequest.UserInput;

                lastChatMessage.CorrectedUserInput = await spellCheckService.SpellcheckAsync(userDataFilterService.FilterUserData(chatModel.CurrentState, lastChatMessage.UserInput, false));

                // Set field variable to last input for flows usage
                chatModel.CurrentState.GlobalAnswers[ChatStandardField.LastUserInput] = lastChatMessage.UserInput;
                chatModel.CurrentState.SessionData.LastUserInput = lastChatMessage.UserInput;
                chatModel.CurrentState.GlobalAnswers[ChatStandardField.LastCorrectedUserInput] = lastChatMessage.CorrectedUserInput;
                chatModel.CurrentState.SessionData.LastCorrectedUserInput = lastChatMessage.CorrectedUserInput;
                chatModel.CurrentState.GlobalAnswers[ChatStandardField.UserAskedWhy] = false;
                chatModel.CurrentState.SessionData.UserAskedWhy = false;
                chatModel.CurrentState.SessionData.DummyMode = chatRequest.DebugData != null ? chatRequest.DebugData.DummyMode : false;

                // Reset flag to show last message
                chatModel.CurrentState.DontShowLastMessage = false;

                if (chatRequest.UserInput == "--testError")
                    throw new ApplicationException("This is a test error message.");

                using (scriptManager = await CreateChatScriptManager(chatModel, isNewChat, externalDataStorageService, nodeJsHubContext, nodeServices, chatConfiguration))
                {
                    if ((chatRequest.DebugData != null) && (chatRequest.DebugData.DummyMode))
                        scriptManager.ScriptHost.DummyMode = true;

                    if (!String.IsNullOrEmpty(chatRequest.DebugData?.StartFlowName))
                    {
                        if (String.IsNullOrEmpty(chatRequest.DebugData.StartFlowId))
                            chatRequest.DebugData.StartFlowId = "1";
                        chatFlowStep = await LoadStep(chatRequest.DebugData.StartFlowName, chatRequest.DebugData.StartFlowId, chatModel);

                        chatModel.CurrentState.Steps.Add(chatFlowStep);
                        chatMessage.Steps.Add(new ChatStepId(chatFlowStep));
                    }
                    else if (chatFlowStep == null)
                    {
                        if (!String.IsNullOrEmpty(chatRequest.ChatId) &&
                          (chatRequest.ChatId != chatModel.ChatId))
                        {
                            logger.InfoFormat("Internal: Session Timeout {0}", chatRequest.ChatId);

                            // client Specified chatId was not known which indicates a session expired
                            chatFlowStep = await LoadStep("SessionTimeout", "1", chatModel);

                            await SendTagEvent(chatModel.CurrentState.SessionData, "session_timeout");
                        }
                        else
                        {
                            // New Chat Session
                            chatFlowStep = await LoadStep("StartChat", "1", chatModel);
                        }
                        chatModel.CurrentState.AddToFlowPath(chatFlowStep.Flow);
                        chatModel.CurrentState.Steps.Add(chatFlowStep);
                        chatMessage.Steps.Add(new ChatStepId(chatFlowStep));

                        await SendTagEvent(chatModel.CurrentState.SessionData, "session_start");
                    }
                    else if (!String.IsNullOrEmpty(chatRequest.UserInput))
                    {
                        if (chatRequest.UserInput.Equals("can we play a game?", StringComparison.CurrentCultureIgnoreCase))
                        {
                            chatFlowStep = await LoadStep("ft-zork", "1", chatModel);
                        }
                        else
                        {
                            lastChatMessage = chatModel.CurrentState.GetLastMessage();
                            chatFlowStep = await ProcessUserInput(chatModel, chatFlowStep, lastChatMessage);
                        }

                        if (chatFlowStep != null)
                        {
                            ProcessChatScriptHostUiMessages(chatModel, chatMessage, chatFlowStep, false);
                            chatModel.CurrentState.Steps.Add(chatFlowStep);
                            chatMessage.Steps.Add(new ChatStepId(chatFlowStep));
                        }
                    }

                    // A blank message is sent after a transfer.  Check if this is a Transfer-Continue scenario
                    if (String.IsNullOrEmpty(chatRequest.UserInput) && (chatModel.CurrentState.Messages.Count > 1))
                    {
                        var lastStep = chatModel.CurrentState.Steps.LastOrDefault();
                        if (lastStep?.Flow == "<Transfer-Continue>")
                            chatFlowStep = await GetChildStepAsync(chatModel, lastStep, null);
                    }

                    chatFlowStep = await ProcessFlowSteps(chatModel, chatFlowStep, chatMessage);

                    if ((chatFlowStep != null) && !String.IsNullOrEmpty(chatFlowStep.GetText()) && !chatModel.CurrentState.DontShowLastMessage)
                    {
                        chatMessage.PlaceholderText = chatFlowStep.PlaceholderText;
                        ProcessChatScriptHostUiMessages(chatModel, chatMessage, chatFlowStep, true);
                    }

                    chatModel.CurrentState.Messages.Add(chatMessage);

                    if (!chatMemCacheService.SaveObject(chatModel.ChatId, chatModel, chatModel.ChatTimeout))
                        logger.ErrorFormat("Internal: Failed to save chat session {0} to cache.", chatModel.ChatId);

                    bool includeDebugData = ((chatRequest.DebugData != null) && chatConfiguration.AllowDebugMode);
                    var response = BuildChatResponse(chatModel, chatMessage, chatFlowStep, includeDebugData);

                    sw.Stop();

                    var message = FormatLogChatMessage(chatRequest.UserInput, chatModel, chatMessage.Steps.ToArray(), sw);
                    if (sw.Elapsed.Seconds > ChatConfiguration.ChatResponseWarnMaxTime)
                        logger.Warn(message);
                    else
                        logger.Info(message);

                    if (kinesisService != null)
                        await kinesisService.SendTranscriptEvent(chatModel, chatRequest.UserInput, sw, chatMessage.Steps.ToArray());

                    return response;
                }
            }
            catch (Exception ex)
            {
                if (chatModel != null)
                {
                    chatModel.IncreaseBadMessageCount();
                    chatMessage.AddUiText(ChatObjectType.StaticMessage, GetServerErrorMessage(chatModel.CurrentState.SessionData.Partner, chatModel.CurrentState.SessionData.Context), null);

                    if (!chatModel.CurrentState.Messages.Contains(chatMessage))
                        chatModel.CurrentState.Messages.Add(chatMessage);
                }

                try
                {
                    if (kinesisService != null)
                        await kinesisService.SendTranscriptEvent(chatModel, chatRequest.UserInput, sw, chatMessage.Steps.ToArray());
                }
                catch(Exception exKinesis)
                {
                    logger.Error("Internal: chaterror", exKinesis);
                }

                bool includeDebugData = ((chatRequest.DebugData != null) && chatConfiguration.AllowDebugMode);
                return CreateChatErrorResponse(chatModel, chatMessage, chatRequest?.UserInput, ex, includeDebugData);
            }
        }

        private static void SetLoggingProperties(ChatModel chatModel, ChatMessage chatMessage)
        {
            var sessionData = chatModel.CurrentState.SessionData;

            // "properties" is a built in log4net field, and if included,
            // all properties are duplicated in it.  so we use "props" and map
            // it in the log4net config file.
            log4net.LogicalThreadContext.Properties["props"] = new LogProperty()
            {
                channel = sessionData.Channel,
                context = sessionData.Context?.ToLower(),
                partner = sessionData.Partner?.ToLower(),
                distinct_id = sessionData.DistinctId,
                utm_campaign = sessionData.UtmCampaign,
                utm_medium = sessionData.UtmMedium,
                utm_source = sessionData.UtmSource
            };

            log4net.LogicalThreadContext.Properties["chatMessageSteps"] = chatMessage.Steps;
            log4net.LogicalThreadContext.Properties["chatId"] = sessionData.ChatId;
            log4net.LogicalThreadContext.Properties["sessionId"] = sessionData.SessionId;

            if (!String.IsNullOrEmpty(sessionData.PartnerContext))
                log4net.LogicalThreadContext.Properties["indexName"] = "log_events_" + sessionData.PartnerContext.ToLower();
        }

        private void ProcessChatScriptHostUiMessages(ChatModel chatModel, ChatMessage chatMessage, ChatFlowStep chatFlowStep, bool allowFlowTextMessage)
        {
            if ((chatFlowStep.SubType == ChatSubType.Debug) && !chatConfiguration.ShowDebugChatMessages)
                return;

            var uiMessages = scriptManager.ScriptHost.GetProperty<List<object>>("UiMessages");

            if (uiMessages != null)
            {
                foreach (object messagePair in uiMessages)
                {
                    (string uiMessage, string plainMessage) = (Tuple<string, string>)messagePair;
                    chatMessage.AddUiText(chatFlowStep.ObjectType, plainMessage, uiMessage);
                }
            }
            else if (allowFlowTextMessage && !IsNodeManager(chatModel))
            {
                var plainMessage = MessageFormatter.FormatMessage(chatModel.CurrentState, chatFlowStep);
                chatMessage.AddUiText(chatFlowStep.ObjectType, plainMessage, null);
            }
        }

        private string GetServerErrorMessage(string partner, string context)
        {
            if (context == "AnywhereExpert")
                return ChatConfiguration.AnywhereExpertServerErrorMessage;

            return ChatConfiguration.DefaultServerErrorMessage;
        }

        private async Task<ChatFlowStep> ProcessUserInput(ChatModel chatModel, ChatFlowStep chatFlowStep, ChatMessage lastChatMessage)
        {
            var processedFields = await ProcessParseFields(chatModel, chatFlowStep, false);

            if (!AreAnyFieldsProcessed(processedFields))
            {
                var commonChatStep = await HandleCommonChat(chatModel, chatFlowStep);

                if (commonChatStep != null)
                {
                    chatModel.ResetConsecutiveBadMessageCount();

                    // Set any parse intent fields as processed
                    processedFields.Where(field => field.Key.ParseType == ChatRuleType.Parse).ToList().ForEach(field => processedFields[field.Key] = true);

                    return commonChatStep;
                }
            }

            ProcessDefaultIntentFields(chatModel, processedFields);

            // continue if any fields were processed
            if (AreAnyFieldsProcessed(processedFields))
            {
                chatModel.ResetConsecutiveBadMessageCount();

                return await GetChildStepAsync(chatModel, chatFlowStep, null);
            }

            var handlePreviousAnswer = await CheckForNewAnswerToPreviousQuestion(chatModel, chatFlowStep, lastChatMessage);
            if (handlePreviousAnswer != null)
                return handlePreviousAnswer;

            if (!chatModel.ContextRule.IgnoreAutoClassify)
            {
                var step = await AutoClassify(chatModel, chatFlowStep, lastChatMessage);
                if (step != null)
                    return step;
            }

            return await HandleUnparsed(chatModel, chatFlowStep, lastChatMessage);
        }

        private static void ProcessDefaultIntentFields(ChatModel chatModel, Dictionary<Chat_ParseField, bool> processedFields)
        {
            processedFields.Where(field => field.Key.ParseType == ChatRuleType.Parse &&
                field.Value == false &&
                !String.IsNullOrEmpty(field.Key.RuleData)).ToList().ForEach(field =>
                {
                    string defaultIntent = field.Key?.GetProperty("DefaultIntent") ?? field.Key.RuleData;

                    if (!String.IsNullOrEmpty(defaultIntent))
                    {
                        chatModel.CurrentState.GlobalAnswers[field.Key.FieldName] = defaultIntent;
                        processedFields[field.Key] = true;
                    }
                });
        }

        private async Task<ChatFlowStep> CheckForNewAnswerToPreviousQuestion(ChatModel chatModel, ChatFlowStep chatFlowStep, ChatMessage lastMessage)
        {
            var checkSteps = (from step in chatModel.CurrentState.Steps.AsEnumerable().Reverse()
                             where !String.IsNullOrEmpty(step.ErrorHandler) &&
                             (step.ParseFields?.Count > 0)
                             select step).Take(1);

            foreach (var questionStep in checkSteps)
            {
                foreach (var field in questionStep.ParseFields)
                {
                    if (field.ParseType != ChatRuleType.PhoneNumber)
                        continue;

                    var match = await ParseMessage(lastMessage, chatModel, chatFlowStep, field, true);
                    if (match.Success)
                    {
                        logger.InfoFormat("Parse: Detected answer to previous question. Current step: {0}-{1}, Step answered: {2}-{3}", chatFlowStep.Flow, chatFlowStep.Id, questionStep.Flow, questionStep.Id);

                        var errorData = new ErrorHandlerData()
                        {
                            ErrorType = ErrorHandlerDataType.DelayedAnswer,
                            FieldName = field.FieldName,
                            FieldAnswer = chatModel.CurrentState.GlobalAnswers[field.FieldName],
                            SourceStep = chatFlowStep
                        };
                        return await ProcessErrorHandler(chatModel, questionStep, errorData);
                    }
                }
            }

            return null;
        }

        private async Task<ChatFlowStep> HandleUnparsed(ChatModel chatModel, ChatFlowStep chatFlowStep, ChatMessage lastChatMessage)
        {
            var nextStep = await ProcessErrorHandler(chatModel,
                chatFlowStep,
                new ErrorHandlerData()
                {
                    ErrorType = ErrorHandlerDataType.NoValueParsed,
                    SourceStep = chatFlowStep
                });

            if (nextStep != null)
                return nextStep;

            if (kinesisService != null)
                await kinesisService.SendFailToUnderstandEvent(chatModel, chatFlowStep);

            return await PushFailureStep(chatModel, chatFlowStep, "FailToUnderstand");
        }

        private async Task<ChatFlowStep> AutoClassify(ChatModel chatModel, ChatFlowStep chatFlowStep, ChatMessage lastChatMessage)
        {
            // TODO: For now, we clear the cached classifications to force a new classification with a higher threshold
            // In reality the classifications will be the same, just our calculation of the ones above the threshold are different.
            // But that affects calling subclassifiers or not.
            // This will all be handled in Intent gateway later, so no need to waste time making it work here.
            lastChatMessage.Classifications = null;

            bool intentFound = await ClassifyText(chatModel, lastChatMessage, null, chatModel.ContextRule?.DefaultClassifier, ChatConfiguration.MinimumConfidenceRatioUnknownClassification, chatFlowStep.AllowRetryClassificationWithBotText);

            if (!intentFound)
                return null;

            bool intentAllowed = DoesContextAllowFlow(chatModel, chatModel.CurrentState.LastClassification?.GetBestResult()?.Intent);
            ChatFlowStep newFlowStep = null;

            if (intentAllowed)
                newFlowStep = await LoadStep(chatModel.CurrentState.LastClassification?.GetBestResult()?.Intent, "1", chatModel);
            else if (!chatModel.ContextRule.IgnoreUnsupportedFlowMessage)
                newFlowStep = await LoadUnsupportedFlow(chatModel);

            if (newFlowStep == null)
                return null;

            // Add to stack, unless we just did the transfer agent question. 
            // It means the user is asking a new question first.
            if (!(chatFlowStep.Flow.StartsWith("TransferAgent") && (chatFlowStep.Id == "1")))
                chatModel.CurrentState.FlowStack.Push(new ChatStackItem(chatFlowStep, null, null));

            return newFlowStep;
        }

        private async Task<bool> ClassifyText(ChatModel chatModel, ChatMessage chatMessage, Chat_ParseField field, string defaultClassifier, double minimumThreshold, bool allowRetryWithBotText)
        {
            if (chatMessage.Classifications != null)
                return chatMessage.Classifications.IsSuccessful;

            IntentParser intentParser = new IntentParser(classificationService, userDataFilterService, minimumThreshold, defaultClassifier);
            var parseResult = await intentParser.ParseAsync(chatModel.CurrentState, field, chatMessage);

            if (kinesisService != null)
                await kinesisService.SendClassificationEvent(chatModel, field, chatMessage.Classifications.IsSuccessful, false, chatMessage.Classifications);

            LogClassificationResult(chatMessage.Classifications);

            return chatMessage.Classifications.IsSuccessful;
        }

        private async Task<ChatFlowStep> LoadUnsupportedFlow(ChatModel chatModel)
        {
            chatModel.CurrentState.GlobalAnswers[ChatStandardField.UnsupportedIntent] = chatModel.CurrentState.LastClassification?.GetBestResult();
            chatModel.CurrentState.SessionData.UnsupportedIntent = chatModel.CurrentState.GlobalAnswers.GetFieldAnswer<ClassificationResponse>(ChatStandardField.UnsupportedIntent);
            return await LoadStep("UnsupportedIntent", "1", chatModel);
        }

        private static bool DoesContextAllowFlow(ChatModel chatModel, string flowName)
        {
            if (String.IsNullOrEmpty(flowName))
                return false;
                
            if (!String.IsNullOrEmpty(chatModel.CurrentState.SessionData.Context))
            {
                var allowFlowRules = new List<string>(ChatConfiguration.AlwaysAllowedFlows);

                if (chatModel.ContextRule.AllowedIntentFlows?.Count > 0)
                    allowFlowRules.AddRange(chatModel.ContextRule.AllowedIntentFlows);

                foreach (var rule in allowFlowRules)
                {
                    if (Regex.IsMatch(flowName, rule))
                        return true;
                }
            }

            return false;
        }

        private async Task<ChatFlowStep> ProcessErrorHandler(ChatModel chatModel, ChatFlowStep chatFlowStep, ErrorHandlerData errorData)
        {
            if (String.IsNullOrEmpty(chatFlowStep.ErrorHandler))
                return null;

            logger.InfoFormat("Calling Error handler. Current step: {0}-{1}", chatFlowStep.Flow, chatFlowStep.Id);

            chatModel.CurrentState.SessionData.ErrorData = errorData;

            var result = await scriptManager.ProcessErrorHandler(chatModel, chatFlowStep);

            if (result == null)
                return null;

            switch (result.ContinueType)
            {
                case ContinueType.CallAndRetry:
                    chatModel.CurrentState.FlowStack.Push(new ChatStackItem(errorData.SourceStep ?? chatFlowStep, null, null));
                    return await LoadStep(result.ChatStepId.FlowName ?? chatFlowStep.Flow, result.ChatStepId.StepId, chatModel);
                case ContinueType.Retry:
                    return chatFlowStep;
                case ContinueType.Jump:
                    var targetFlow = result.ChatStepId.FlowName ?? chatFlowStep.Flow;
                    if (targetFlow != chatFlowStep.Flow)
                    {
                        logger.ErrorFormat("Script: Illegal jump.  Error handlers should not use Jump result to jump to another flow.  It should only jump to another step within the same flow. Flow: {0} Step: {1}", chatFlowStep.Flow, chatFlowStep.Id);
                        throw new ApplicationException($"Illegal jump.  Error handlers cannot use Jump result to jump to another flow. Flow: {chatFlowStep.Flow} Step: {chatFlowStep.Id}");
                    }

                    return await LoadStep(targetFlow, result.ChatStepId.StepId, chatModel);
                case ContinueType.Continue:
                    return errorData.SourceStep;
                case ContinueType.Ignore:
                    return await GetChildStepAsync(chatModel, chatFlowStep, null);
            }

            return null;
        }

        private async Task<ChatFlowStep> ProcessFlowSteps(ChatModel chatModel, ChatFlowStep chatFlowStep, ChatMessage chatMessage)
        {
            int currentStepCount = 0;

            while (!chatModel.PassThru && (chatFlowStep != null)) //(step.ObjectType == ChatObjectType.StaticMessage) || (step.ObjectType == ChatObjectType.Analyze) || (step.ObjectType == ChatObjectType.Connection) || (step.ObjectType == ChatObjectType.Message))
            {
                // Process actions first, since message objects can also contains scripts now.
                await ProcessActions(chatModel, chatFlowStep);

                if (scriptManager.ScriptHost.ContainsProperty("TrustedClientResponse"))
                    chatModel.CurrentState.TrustedClientResponse = scriptManager.ScriptHost.GetProperty<Dictionary<string, object>>("TrustedClientResponse");

                // Try parsing fields from previous messages
                if ((chatFlowStep.ObjectType == ChatObjectType.Message) || (chatFlowStep.ObjectType == ChatObjectType.UIElement))
                {
                    var processedFields = await ProcessParseFields(chatModel, chatFlowStep, true);
                    if ((chatFlowStep.ParseFields?.Count > 0) && !AreAnyFieldsProcessed(processedFields))
                        break;
                }

                if ((chatFlowStep.ObjectType == ChatObjectType.StaticMessage) || (chatFlowStep.ObjectType == ChatObjectType.UIElement) || (chatFlowStep.ObjectType == ChatObjectType.Analyze))
                    ProcessChatScriptHostUiMessages(chatModel, chatMessage, chatFlowStep, true);

                if (scriptManager.ScriptHost.GetProperty<bool>("TransferToAgent") == true)
                {
                    chatFlowStep = null; // End Chat
                    chatModel.CurrentState.GlobalAnswers[ChatStandardField.TransferToAgent] = true;
                    chatModel.CurrentState.SessionData.TransferToAgent = true;
                    chatModel.CurrentState.TransferToAgent = true;
                    chatModel.CurrentState.TransferToAgentSkill = scriptManager.ScriptHost.GetProperty<string>("TransferToAgentSkill");
                    TransferInfo = scriptManager.ScriptHost.GetProperty("TransferInfo");
                    break;
                }

                // Check if the script flagged a failure
                if (scriptManager.ScriptHost.GetProperty<bool>("IncreaseFailureCount"))
                {
                    if (CheckTooManyFails(chatModel))
                    {
                        chatFlowStep = await LoadStep("TransferAgent", "1", chatModel);
                        break;
                    }
                }

                chatFlowStep = await GetChildStepAsync(chatModel, chatFlowStep, null);

                if (chatFlowStep == null)
                    break;

                chatModel.CurrentState.Steps.Add(chatFlowStep);

                if (chatFlowStep.Flow == "<Transfer-Continue>")
                    break;

                chatMessage.Steps.Add(new ChatStepId(chatFlowStep));
                if (currentStepCount++ > ChatConfiguration.MaximumStepCount)
                {
                    chatFlowStep = await LoadStep("BadFlow", "9999997", chatModel);
                    break;
                }
            }

            return chatFlowStep;
        }

        private ChatResponse BuildChatResponse(ChatModel chatModel, ChatMessage chatMessage, ChatFlowStep chatFlowStep, bool includeDebugData)
        {
            var response = new ChatResponse()
            {
                ChatId = chatModel.ChatId,
                AgentName = chatModel.CurrentState.GlobalAnswers.GetFieldAnswer(ChatStandardField.AgentName)
            };

            response.TrustedClientData = chatModel.CurrentState.TrustedClientResponse;
            response.Messages = chatMessage.BotQuestionsTextList;
            response.UiMessages = chatMessage.BotQuestions;
            response.Flows = chatModel.CurrentState.FlowPath.ToArray();
            response.Steps = chatMessage.Steps.ToArray();
            response.Version = appVersion;
            if (chatFlowStep != null)
            {
                response.PossibleUserAnswers = GetExpectedAnswers(chatFlowStep);
                var scriptQuickReplies = scriptManager.ScriptHost.GetProperty<UserChoice[]>("QuickReplyChoices");
                if (scriptQuickReplies != null)
                    response.UserChoices = scriptQuickReplies;
                else if (chatFlowStep.UserChoices != null)
                    response.UserChoices = chatFlowStep.UserChoices.ToArray();
            }

            response.PlaceholderText = chatMessage.PlaceholderText;
            response.UserInputType = GetExpectedInputType(chatFlowStep);
            response.Status = GetChatStatus(chatModel, chatMessage, chatFlowStep);

            // Set old properties for backward compatibility
            response.ShouldEndChat = response.Status.ShouldEndChat;
            response.TransferToAgent = response.Status.TransferToAgent;
            if (response.TransferToAgent)
                response.TransferInfo = TransferInfo;

            if (includeDebugData)
            {
                response.DebugData = new DebugDataResponse
                {
                    Variables = chatModel.CurrentState.GlobalAnswers.FieldAnswers,
                    LocalVariables = chatModel.CurrentState.FlowAnswers.AsEnumerable().ToDictionary(r => r.Key, r => r.Value.FieldAnswers),
                    Flows = chatModel.CurrentState.FlowPath.ToArray(),
                    Steps = chatMessage.Steps.ToArray()
                };
            }

            return response;
        }

        private ChatStatus GetChatStatus(ChatModel chatModel, ChatMessage chatMessage, ChatFlowStep chatFlowStep)
        {
            bool transferToAgent = chatModel.CurrentState.TransferToAgent;

            return new ChatStatus
            {
                FlowPathRestarted = (from step in chatMessage.Steps
                                     where step.FlowName.StartsWith("ContinueChat") && step.StepId == "1"
                                     select step).Count() > 0,
                TransferToAgent = transferToAgent,
                TransferInfo = transferToAgent ? TransferInfo : null,
                TransferToAgentSkill = transferToAgent ? chatModel.CurrentState.TransferToAgentSkill : null,
                WaitTimeForNextAgent = transferToAgent ? GetLiveAgentWaitTime(chatModel.CurrentState, chatModel.CurrentState.TransferToAgentSkill) : -1,
                ShouldEndChat = chatFlowStep == null && !transferToAgent,

                FailToUnderstand = (from step in chatMessage.Steps
                                    where step.FlowName.StartsWith("FailToUnderstand")
                                    select step).Count() > 0,

                SessionExpired = (from step in chatMessage.Steps
                                  where step.FlowName.StartsWith("SessionTimeout")
                                  select step).Count() > 0,
            };
        }

        private static void ValidateInput(ChatRequest chatRequest)
        {
            if (chatRequest == null)
                throw new HttpResponseException(HttpStatusCode.BadRequest);

            if ((chatRequest.Channel != "sms") && chatRequest.ChatTimeout > ChatConfiguration.MaximumChatTimeoutMinutes)
                throw new HttpResponseException(HttpStatusCode.BadRequest);

            if ((chatRequest.Channel == "sms") && chatRequest.ChatTimeout > ChatConfiguration.MaximumSmsChatTimeoutMinutes)
                throw new HttpResponseException(HttpStatusCode.BadRequest);

            if (chatRequest.UserInput?.Length > ChatConfiguration.MaximumTextInput)
            {
                logger.WarnFormat("User input longer than allowed.  Truncating from {0} to {1} characters.", chatRequest.UserInput?.Length, ChatConfiguration.MaximumTextInput);
                chatRequest.UserInput = chatRequest.UserInput.Substring(0, ChatConfiguration.MaximumTextInput);
            }
        }

        private ChatModel GetCurrentChat(string chatId)
        {
            if (String.IsNullOrEmpty(chatId))
                return null;

            return chatMemCacheService.GetObject<ChatModel>(chatId);
        }

        private async Task<ChatModel> CreateNewChat(ChatRequest chatRequest)
        {
            var chat = new ChatModel(chatRequest.AgentFirstName);
            chat.CurrentState.ChangePartnerContext(chatRequest.Partner, chatRequest.Context);

            if (String.IsNullOrEmpty(chatRequest.Context))
                chat.ContextRule = new ContextRule();
            else 
                chat.ContextRule = await externalDataStorageService.GetContextRule(chatRequest.Context) ?? new ContextRule();

            return chat;
        }

        private void HandleChatParams(ChatRequest chatRequest, ChatModel chatModel)
        {
            // Set channel for current post.  The channel can change during a conversation
            if (!String.IsNullOrEmpty(chatRequest.Channel))
            {
                chatModel.CurrentState.GlobalAnswers[ChatStandardField.Channel] = chatRequest.Channel;
                chatModel.CurrentState.SessionData.Channel = chatRequest.Channel;

                if (chatRequest.Channel != "chatweb")
                    chatModel.CurrentState.GlobalAnswers[ChatStandardField.DelayPrompt] = "";
                else
                    chatModel.CurrentState.GlobalAnswers[ChatStandardField.DelayPrompt] = "<DelayPrompt>";
                chatModel.CurrentState.SessionData.DelayPrompt = chatModel.CurrentState.GlobalAnswers.GetFieldAnswer(ChatStandardField.DelayPrompt);
            }

            if (chatRequest.ChannelData != null)
            {
                chatModel.CurrentState.ChannelData = chatRequest.ChannelData;
                logger.DebugFormat("Internal: channeldata set id: {0}, tagprefix: {1}", chatRequest.ChannelData.Id, chatRequest.ChannelData.TagPrefix);
            }

            if (!String.IsNullOrEmpty(chatRequest.InputType))
            {
                chatModel.CurrentState.GlobalAnswers[ChatStandardField.InputType] = chatRequest.InputType;
                chatModel.CurrentState.SessionData.InputType = chatRequest.InputType;
            }

            if (chatRequest.Channel == "sms")
                chatModel.ChatTimeout = ChatConfiguration.DefaultSmsChatTimeoutMinutes;

            if (chatRequest.ChatTimeout > 0)
                chatModel.ChatTimeout = chatRequest.ChatTimeout;

            chatModel.AgentPaused = chatRequest.AgentPaused;

            if (!chatModel.CurrentState.GlobalAnswers.IsFieldAnswered(ChatStandardField.Environment))
            {
                var env = new Dictionary<string, object>
                {
                    ["Name"] = chatConfiguration.ChatEnvironmentName
                };
                chatModel.CurrentState.GlobalAnswers[ChatStandardField.Environment] = env;
                chatModel.CurrentState.SessionData.Environment = env;
            }

            if (!chatModel.CurrentState.GlobalAnswers.IsFieldAnswered(ChatStandardField.SessionId))
            {
                chatModel.CurrentState.GlobalAnswers[ChatStandardField.SessionId] = chatModel.ChatId;
                chatModel.CurrentState.SessionData.ChatId = chatModel.ChatId;
            }

            if (!String.IsNullOrEmpty(chatRequest.UserName))
            {
                if (chatRequest.Channel == "sms")
                {
                    // Strip +1 for now, but parser should be updated to handle it
                    if (!chatModel.CurrentState.GlobalAnswers.IsFieldAnswered(ChatStandardField.PhoneNumber))
                    {
                        chatModel.CurrentState.GlobalAnswers[ChatStandardField.PhoneNumber] = chatRequest.UserName.Replace("+1", "");
                        chatModel.CurrentState.SessionData.PhoneNumber = chatModel.CurrentState.GlobalAnswers.GetFieldAnswer(ChatStandardField.PhoneNumber);
                    }
                }
                else
                {
                    if (!chatModel.CurrentState.GlobalAnswers.IsFieldAnswered(ChatStandardField.Name))
                    {
                        chatModel.CurrentState.GlobalAnswers[ChatStandardField.Name] = chatRequest.UserName;
                        chatModel.CurrentState.SessionData.Name = chatRequest.UserName;
                    }
                }
            }

            if (!chatModel.CurrentState.GlobalAnswers.IsFieldAnswered(ChatStandardField.UsersTimeZoneOffset))
            {
                chatModel.CurrentState.GlobalAnswers[ChatStandardField.UsersTimeZoneOffset] = GetUserTimeZone(chatRequest, chatModel);
                chatModel.CurrentState.SessionData.UsersTimeZoneOffset = chatModel.CurrentState.GlobalAnswers.GetFieldAnswer(ChatStandardField.UsersTimeZoneOffset);
            }

            HandleChatClientDataParams(chatRequest, chatModel);

            if (chatRequest.TrustedClientData != null)
            {
                chatModel.CurrentState.GlobalAnswers[ChatStandardField.TrustedClientData] = chatRequest.TrustedClientData.FromScriptValue();
                if (chatRequest.TrustedClientData.ContainsKey("SessionId"))
                    chatModel.ClientSessionId = chatRequest.TrustedClientData["SessionId"] as string;
            }

            chatModel.CurrentState.SessionData.SessionId = chatModel.SessionLogId;
        }

        private static int GetUserTimeZone(ChatRequest chatRequest, ChatModel chatModel)
        {
            if (!String.IsNullOrEmpty(chatRequest.TimeZone) && (int.TryParse(chatRequest.TimeZone, out int offset)))
                return offset / 100;

            return ChatConfiguration.TimeZoneOffset;
        }

        private void HandleChatClientDataParams(ChatRequest chatRequest, ChatModel chatModel)
        {
            if (chatRequest.ClientData == null)
            {
                chatRequest.ClientData = new ChatClientData()
                {
                    EstimatedLiveAgentWaitTime = -1,
                };
            }

            HandleChatClientDataParamsLegacy(chatRequest.ClientData, chatModel);

            chatModel.CurrentState.GlobalAnswers[ChatStandardField.ClientData] = new ClientData()
            {
                ArticleId = chatRequest.ClientData.ArticleId,
                ClientName = chatRequest.ClientData.ClientName,
                ClientVersion = chatRequest.ClientData.ClientVersion,
                Fingerprint = chatRequest.ClientData.Fingerprint,
                ClientIp = chatRequest.ClientData.ClientIp,
                IsDesktopUser = chatRequest.ClientData.IsDesktopUser,
                SubscriberMdn = chatRequest.ClientData.SubscriberMdn,
                AlternateMdn = chatRequest.ClientData.AlternateMdn,
                UserAgent = chatRequest.ClientData.UserAgent,
                TimeZoneOffset = GetUserTimeZone(chatRequest, chatModel),
                UiResult = chatRequest.ClientData.UiResult
            };

        }

        private void HandleChatClientDataParamsLegacy(ChatClientData chatClientData, ChatModel chatModel)
        {
            ProcessLiveAgentWaitTimes(chatModel.CurrentState, chatClientData);

            if (chatClientData.ClaimsPod != null)
                chatModel.CurrentState.GlobalAnswers[ChatStandardField.ClaimsPod] = chatClientData.ClaimsPod;
            if (chatClientData.UtmSource != null)
            {
                chatModel.CurrentState.GlobalAnswers[ChatStandardField.UtmSource] = chatClientData.UtmSource;
                chatModel.CurrentState.SessionData.UtmSource = chatClientData.UtmSource;
            }
            if (chatClientData.UtmMedium != null)
            {
                chatModel.CurrentState.GlobalAnswers[ChatStandardField.UtmMedium] = chatClientData.UtmMedium;
                chatModel.CurrentState.SessionData.UtmMedium = chatClientData.UtmMedium;
            }
            if (chatClientData.UtmCampaign != null)
            {
                chatModel.CurrentState.GlobalAnswers[ChatStandardField.UtmCampaign] = chatClientData.UtmCampaign;
                chatModel.CurrentState.SessionData.UtmCampaign = chatClientData.UtmCampaign;
            }
            if (chatClientData.Distinct_Id != null)
            {
                chatModel.CurrentState.GlobalAnswers[ChatStandardField.DistinctId] = chatClientData.Distinct_Id;
                chatModel.CurrentState.SessionData.DistinctId = chatClientData.Distinct_Id;
            }
            if (chatClientData.Fingerprint != null)
            {
                chatModel.CurrentState.GlobalAnswers[ChatStandardField.Fingerprint] = chatClientData.Fingerprint;
                chatModel.CurrentState.SessionData.Fingerprint = chatClientData.Fingerprint;
            }
        }

        void ProcessLiveAgentWaitTimes(ChatState chatState, ChatClientData clientData)
        {
            chatState.GlobalAnswers[ChatStandardField.EstimatedLiveAgentWaitTime] = clientData.EstimatedLiveAgentWaitTime;
            chatState.SessionData.EstimatedLiveAgentWaitTime = clientData.EstimatedLiveAgentWaitTime;

            var waitTimes = new Dictionary<string, int>
            {
                ["default"] = clientData.EstimatedLiveAgentWaitTime
            };

            if (clientData.Properties != null)
            {
                foreach (var prop in clientData.Properties)
                {
                    if (prop.Key.StartsWith("EstimatedLiveAgentWaitTime"))
                    {
                        var keys = prop.Key.Split(splitWaitTime, 2, StringSplitOptions.RemoveEmptyEntries);
                        if (keys.Length != 2)
                            continue;

                        waitTimes[keys[1]] = prop.Value.ToObject<int>();
                    }
                }
            }

            chatState.GlobalAnswers[ChatStandardField.EstimatedLiveAgentWaitTimes] = waitTimes;
            chatState.SessionData.EstimatedLiveAgentWaitTimes = waitTimes;
        }

        void ProcessDebugVariables(ChatState chatState, Dictionary<string, object> variables)
        {
            foreach (var variable in variables)
            {
                chatState.GlobalAnswers[variable.Key] = variable.Value;
            }
        }

        async Task<ChatFlowStep> HandleCommonChat(ChatModel chatModel, ChatFlowStep chatFlowStep)
        {
            bool success = await ClassifyText(chatModel, chatModel.CurrentState.GetLastMessage(), null, TextClassificationService.CommonChatCategory, ChatConfiguration.MininimConfidenceRatio, false);

            if (chatModel.CurrentState.LastClassification.GetBestResult()?.Intent == "commonchat-TellWhy")
            {
                userAskedWhy = true;
                chatModel.CurrentState.GlobalAnswers[ChatStandardField.UserAskedWhy] = true;
                chatModel.CurrentState.SessionData.UserAskedWhy = true;

                // TODO: "Why" has limited use now, but if "Why" is expected in more places in the flow,
                // then this will cause unnecessary transfers
                if (CheckTooManyFails(chatModel))
                    return await LoadStep("TransferAgent", "1", chatModel);

                // TODO:  What if current flow step doesn't support "Why", we need to fallback to something else?
                return await GetChildStepAsync(chatModel, chatFlowStep, null);
            }

            if (chatModel.CurrentState.LastClassification?.GetBestResult()?.Intent == "commonchat-Breakout-Solution")
            {
                logger.Info("Internal: Breaking out of current flows (commonchat-Breakout-Solution detected)");

                // TODO: Handle in flow with new <End-All>
                chatModel.CurrentState.FlowStack.Clear();

                return await LoadStep("ContinueChat", "1", chatModel);
            }

            // Wait, UserReturnedFromWait, Robot
            var isCommonChat = chatModel.CurrentState.LastClassification?.GetBestResult()?.Intent?.StartsWith("commonchat-");
            if (isCommonChat == true) 
            {
                chatModel.CurrentState.FlowStack.Push(new ChatStackItem(chatFlowStep, null, null));

                return await LoadStep(chatModel.CurrentState.LastClassification?.GetBestResult().Intent, "1", chatModel);
            }

            return null;
        }

        private ChatResponse CreateChatErrorResponse(ChatModel chatModel, ChatMessage chatMessage, string userInput, Exception ex, bool includeDebugData)
        {
            StringBuilder sbError = new StringBuilder();
            sbError.Append("Internal: Chat Response Error: input: \"");
            sbError.Append(userDataFilterService.FilterUserData(chatModel?.CurrentState, userInput));

            sbError.Append("\" Error: ");
            sbError.Append(ex.ToString());

            logger.ErrorFormat(sbError.ToString());

            string agentName = chatModel?.CurrentState?.GlobalAnswers.GetFieldAnswer(ChatStandardField.AgentName) ?? "Ava";
            List<UiMessage> errorMessages = new List<UiMessage>
            {
                new UiMessage() { MessageType = "Message", TextMessage = GetServerErrorMessage(chatModel?.CurrentState?.SessionData?.Partner, chatModel?.CurrentState?.SessionData?.Context) }
            };

            if (chatConfiguration.ShowDebugChatMessages)
                errorMessages.Add(new UiMessage() { MessageType = "Message", TextMessage = ex.ToString() });

            var chatResponse = new ChatResponse()
            {
                Messages = errorMessages.Select(x => x.TextMessage).ToArray(),
                UiMessages = errorMessages.ToArray(),
                ChatId = chatModel?.ChatId,
                AgentName = agentName,
                Error = ex.ToString(),
                Version = appVersion,
                Steps = chatMessage.Steps.ToArray(),
                Status = new ChatStatus()
                {
                    ShouldEndChat = true,
                    Error = ex.ToString()
                },
                ShouldEndChat = true
            };

            if (includeDebugData && chatModel != null)
            {
                try
                {
                    chatResponse.DebugData = new DebugDataResponse
                    {
                        Variables = chatModel.CurrentState.GlobalAnswers.FieldAnswers,
                        LocalVariables = chatModel.CurrentState.FlowAnswers.AsEnumerable().ToDictionary(r => r.Key, r => r.Value.FieldAnswers),
                        Flows = chatModel.CurrentState.FlowPath.ToArray(),
                        Steps = chatMessage.Steps.ToArray()
                    };
                }
                catch (Exception)
                {
                    // Ignore any failures setting debug data
                }
            }

            return chatResponse;
        }

        private string FormatLogChatMessage(string userInput, ChatModel chatModel, ChatStepId[] chatStepIds, Stopwatch stopWatch)
        {
            StringBuilder sbLog = new StringBuilder();

            sbLog.Append("Internal: Response: time: ");
            sbLog.Append(stopWatch.ElapsedMilliseconds);
            sbLog.Append(" input: \"");

            // Clean user data from user input
            sbLog.Append(userDataFilterService.FilterUserData(chatModel?.CurrentState, userInput));
            sbLog.Append("\" bot: \"");

            StringBuilder botText = new StringBuilder();
            foreach (var t in chatModel.CurrentState.GetLastMessage().BotQuestions)
            {
                if (!String.IsNullOrEmpty(t.UiTextMarkup))
                    botText.Append(t.UiTextMarkup);
                else
                    botText.Append(t.TextMessage);
                botText.Append(" ");
            }

            // Clean user data from response
            sbLog.Append(userDataFilterService.FilterUserData(chatModel?.CurrentState, botText.ToString()));

            return sbLog.ToString();
        }

        string[] GetExpectedAnswers(ChatFlowStep chatFlowStep)
        {
            // Use steps Quick Replies first if present
            if (chatFlowStep.QuickReplies?.Count > 0)
                return chatFlowStep.QuickReplies.ToArray();

            if (chatFlowStep.ParseFields == null)
                return null;

            var yesno = (from p in chatFlowStep.ParseFields
                       where p.ParseType == ChatRuleType.YesNoParser && !p.IgnoreQuickReplies
                         select p).FirstOrDefault();

            var continueParser = (from p in chatFlowStep.ParseFields
                                  where p.ParseType == ChatRuleType.ContinueParser && !p.IgnoreQuickReplies
                                  select p).FirstOrDefault();

            var lossCategoryParser = (from p in chatFlowStep.ParseFields
                                  where p.ParseType == ChatRuleType.LossCategoryParserOptions && !p.IgnoreQuickReplies
                                      select p).FirstOrDefault();

            if (yesno != null)
                return new string[] { "Yes", "No" };

            if (continueParser != null)
                return new string[] { "OK" };

            var emailProvider = (from p in chatFlowStep.ParseFields
                         where p.FieldName == "EmailProvider" && !p.IgnoreQuickReplies
                                 select p).FirstOrDefault();

            if (emailProvider != null)
                return new string[] { "Gmail", "Yahoo", "Outlook" };


            var backupProvider = (from p in chatFlowStep.ParseFields
                         where p.ParseType == ChatRuleType.ParseBackupProvider && !p.IgnoreQuickReplies
                                  select p).FirstOrDefault();

            if (backupProvider != null)
                return new string[] { "Apple iCloud", "Google" };

            if (lossCategoryParser != null)
                return new string[] { "Contact with liquid", "Physical Damage", "Lost", "Stolen" };

            return null;
        }

        string GetExpectedInputType(ChatFlowStep chatFlowStep)
        {
            string inputType = null;

            if (chatFlowStep == null)
                return null;

            if (!String.IsNullOrEmpty(chatFlowStep.UserInputType))
                return chatFlowStep.UserInputType;

            if (chatFlowStep.ParseFields == null)
                return null;

            foreach (var field in chatFlowStep.ParseFields)
            {
                var explicitInputType = field.RuleDataObject?.UserInputType;
                if (!String.IsNullOrEmpty(explicitInputType))
                    return explicitInputType;

                switch (field.ParseType)
                {
                    case ChatRuleType.ParseCarrier: inputType = "Carrier"; break;
                    case ChatRuleType.ParseRetailPartner: inputType = "RetailPartner"; break;
                    case ChatRuleType.ParseDevice: inputType = "Device"; break;
                    case ChatRuleType.PhoneNumberNoHistory:
                    case ChatRuleType.PhoneNumber: inputType = "PhoneNumber"; break;
                    case ChatRuleType.Email: inputType = "Email"; break;
                    case ChatRuleType.FuzzyMatch:
                    case ChatRuleType.NameParse:
                    case ChatRuleType.NameParseNoHistory: inputType = "Name"; break;
                    case ChatRuleType.DateParser: 
                    case ChatRuleType.DateParserV2: inputType = "Date"; break;

                    case ChatRuleType.ZipCodeParser: inputType = "Zip"; break;
                    case ChatRuleType.AddressParser: inputType = "Address"; break;
                    case var testField when ((testField == ChatRuleType.Regex) && 
                        String.Equals(field.FieldName, "Address", StringComparison.OrdinalIgnoreCase)): 
                        inputType = "Address";  break;
                    default: inputType = "Text"; break;
                }
            }

            return inputType;
        }

        int GetLiveAgentWaitTime(ChatState chatState, string skill)
        {
            if (String.IsNullOrEmpty(skill))
                return chatState.GlobalAnswers.GetFieldAnswer<int>(ChatStandardField.EstimatedLiveAgentWaitTime);

            var times = chatState.GlobalAnswers.GetFieldAnswer<Dictionary<string, int>>(ChatStandardField.EstimatedLiveAgentWaitTimes);
            if (times.ContainsKey(skill))
                return times[skill];

            return -1;
        }

        /// <summary>
        /// Gets next child step depending on matching conditions.
        /// </summary>
        /// <returns>Id of next child step</returns>
        async Task<ChatFlowStep> GetChildStepAsync(ChatModel chatModel, ChatFlowStep chatFlowStep, List<Chat_ChildRef> chatChildRefList)
        {
            // Check if the script action indicated a child step to call
            var stepId = scriptManager.ScriptHost.GetProperty<ChatStepId>("ChildStepId");
            if (stepId != null)
            {
                var newFlowName = stepId.FlowName ?? chatFlowStep.Flow;

                if (stepId.FlowName != chatFlowStep.Flow)
                {
                    var childRef = new Chat_ChildRef()
                    {
                        FlowName = newFlowName,
                        Id = stepId.StepId,
                        Method = "Call"
                    };

                    var resultPlaceholder = scriptManager.ScriptHost.GetProperty<FlowResult>("ChildResultPlaceholder");
                    chatModel.CurrentState.FlowStack.Push(new ChatStackItem(chatFlowStep, childRef, resultPlaceholder));
                }

                return await LoadStep(newFlowName, stepId.StepId, chatModel);
            }

            if (chatChildRefList == null)
                chatChildRefList = chatFlowStep.Children;

            if (chatChildRefList?.Count > 0)
            {

                string defaultFlow = chatFlowStep.Flow;

                // Order children by their priority flag so they are processed in the right order
                chatChildRefList = chatChildRefList.OrderByDescending(c => c.Priority).ToList();

                foreach (var child in chatChildRefList)
                {
                    if (await scriptManager.IsConditionMet(chatModel, chatFlowStep, child))
                    {
                        var flow = child.FlowName;

                        if (String.IsNullOrEmpty(flow))
                            flow = defaultFlow;

                        // Push the current step and child to the stack to go back to later
                        if (child.Method == "Call")
                            chatModel.CurrentState.FlowStack.Push(new ChatStackItem(chatFlowStep, child, null));

                        switch (flow)
                        {
                            case "<End>":
                            case "<EndFlow>":  // New version
                                {
                                    // End of flow, pop back to previous flow, and check its other children
                                    return await HandleEndOfFlow(chatModel, chatFlowStep, 1);
                                }
                            case "<End-End>":
                                {
                                    // End of flow, pop back 2 levels
                                    // End of flow, pop back to previous flow, and check its other children
                                    return await HandleEndOfFlow(chatModel, chatFlowStep, 2);
                                }
                            case "<End-Redo>":
                                {
                                    // End of flow, pop back to previous flow, and redo it without going to children yet
                                    if (chatModel.CurrentState.FlowStack.Count == 0)
                                        return await LoadStep("ContinueChat", "1", chatModel);

                                    chatModel.CurrentState.RemoveFlowPath(chatFlowStep.Flow);
                                    return chatModel.CurrentState.FlowStack.Pop().Step;
                                }
                            case "<End-Redo-NoMessage>":
                                {
                                    // End of flow, pop back to previous flow, and redo it without sending any message
                                    // This is used in cases when the user says "please wait".  we don't 
                                    // want to repeat our previous question.
                                    if (chatModel.CurrentState.FlowStack.Count == 0)
                                        return await LoadStep("ContinueChat", "1", chatModel);

                                    chatModel.CurrentState.RemoveFlowPath(chatFlowStep.Flow);
                                    chatFlowStep = chatModel.CurrentState.FlowStack.Pop().Step;
                                    chatModel.CurrentState.DontShowLastMessage = true;
                                    
                                    return chatFlowStep;
                                }
                            case "<Transfer-Continue>":
                                {
                                    // Return fake flow step so the main chat engine can handle special case
                                    return new ChatFlowStep() { Flow = flow, Id = "0" };
                                }
                            case "<EndChat>":
                                {
                                    return null;
                                }
                            case "<Transfer>":  // Flows dont create this anymore?
                                {
                                    chatModel.CurrentState.GlobalAnswers[ChatStandardField.TransferToAgent] = true;
                                    chatModel.CurrentState.SessionData.TransferToAgent = true;
                                    return null;
                                }
                            default:
                                {
                                    var formattedFlow = MessageFormatter.FormatMessage(chatModel.CurrentState, chatFlowStep.Flow, flow);
                                    bool isVariableNameFlow = flow.Contains("%");
                                    if (isVariableNameFlow && !DoesContextAllowFlow(chatModel, formattedFlow))
                                        return await LoadUnsupportedFlow(chatModel);

                                    chatModel.CurrentState.AddToFlowPath(formattedFlow);
                                    return await LoadStep(formattedFlow, child.Id, chatModel);
                                }
                        }
                    }
                    
                }
            }
            else
            {
                // No more children, so try to pop back to previous flow.
                return await HandleEndOfFlow(chatModel, chatFlowStep, 1);
            }
            
            if (userAskedWhy)
            {
                chatModel.CurrentState.FlowStack.Push(new ChatStackItem(chatFlowStep, null, null));

                return await LoadStep("commonchat-TellWhy", "1", chatModel);
            }

            // No defined condition found so probably didn't understand user
            // So call "Didnt understand flow"
            logger.ErrorFormat("Flow: Bad Flow conditions.  Flow step had no valid conditions: {0}-{1}", chatFlowStep.Flow, chatFlowStep.Id);
            return await PushFailureStep(chatModel, chatFlowStep, "BadFlowNoCondition"); 
        }

        private async Task<ChatFlowStep> HandleEndOfFlow(ChatModel chatModel, ChatFlowStep chatFlowStep, int popCount)
        {
            // If no more flows, then ask for anything else
            if (chatModel.CurrentState.FlowStack.Count < popCount)
                return await LoadStep("ContinueChat", "1", chatModel);

            ChatStackItem previousStep = null;
            for (int i = 0; i < popCount; i++)
            {
                chatModel.CurrentState.RemoveFlowPath(chatFlowStep.Flow);
                previousStep = chatModel.CurrentState.FlowStack.Pop();
            }

            // Child would be null, if we jumped flows unexpectedly with
            // an unknown question from the user. so just redo the previous step
            // before we jumped
            if (previousStep.Child == null)
                return previousStep.Step;

            // Check for flow return params
            if (previousStep.ResultPlaceholder != null)
                await scriptManager.NotifyFlowEnd(chatModel, previousStep);

            return await GetChildStepAsync(chatModel, previousStep.Step, previousStep.Child.Children);
        }

        async Task<Dictionary<Chat_ParseField, bool>> ProcessParseFields(ChatModel chatModel, ChatFlowStep flowStep, bool searching)
        {
            var parsedFields = new Dictionary<Chat_ParseField, bool>();

            if ((flowStep?.ParseFields == null) || (flowStep.ParseFields.Count == 0))
                return parsedFields;

            foreach (var field in flowStep.ParseFields)
            {
                // Don't do any searching unless the field explicitly allows it.
                // Searching is done to answer a question from data user previously told us in the chat BEFORE asking the question.
                if (searching && (!field.CheckPreviousMessages || !IsSearchableParseType(field)))
                    continue;

                int maxPreviousMessage = Math.Min(chatModel.CurrentState.Messages.Count, ChatConfiguration.MaximumMessageBackSearch);
                if (!searching)
                    maxPreviousMessage = 1;  // Only look at current message
                
                ParseResult parsed = ParseResult.Failed;

                // Look at previous messages from user for answers
                for (int i = 0; i < maxPreviousMessage; i++)
                {
                    var message = chatModel.CurrentState.Messages[chatModel.CurrentState.Messages.Count - i - 1];

                    if (String.IsNullOrEmpty(message.UserInput))
                        continue;

                    // if field exists, clear it in case parsing fails and we don't reuse old answer
                    if (chatModel.CurrentState.GlobalAnswers.FieldAnswers.ContainsKey(field.FieldName))
                        chatModel.CurrentState.GlobalAnswers.FieldAnswers.Remove(field.FieldName);

                    bool searchMode = (searching || (i > 0));

                    parsed = await ParseMessage(message, chatModel, flowStep, field, searchMode);

                    if (parsed.Success && searchMode)
                        AddFieldToAutoFillList(chatModel.CurrentState, field.FieldName);

                    if (parsed.Success)
                        break;
                }

                parsedFields.Add(field, parsed.Success);
            }

            return parsedFields;
        }

        /// <summary>
        /// Searchable means the engine can look at previous user messages for the answer before asking the question with this parse type.
        /// </summary>
        /// <param name="chatParseField"></param>
        /// <returns></returns>
        static bool IsSearchableParseType(Chat_ParseField chatParseField)
        {
            switch (chatParseField.ParseType)
            {
                case ChatRuleType.YesNoParser:
                case ChatRuleType.ContinueParser:
                case ChatRuleType.PauseParser:
                case ChatRuleType.NameParseNoHistory:
                case ChatRuleType.PhoneNumberNoHistory:
                case var testField when (testField == ChatRuleType.Regex && chatParseField.RuleData == ".*"):
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="chatMessage"></param>
        /// <param name="chatModel"></param>
        /// <param name="flowStep"></param>
        /// <param name="chatParseField"></param>
        /// <param name="searchMode">Set to true if we are searching previous user answers to look for matches</param>
        /// <returns></returns>
        private async Task<ParseResult> ParseMessage(ChatMessage chatMessage, ChatModel chatModel, ChatFlowStep flowStep, Chat_ParseField chatParseField, bool searchMode)
        {
            bool isSourceMessageEmpty = ((chatParseField.SourceData == ChatSource.CorrectedInput) && String.IsNullOrEmpty(chatMessage.CorrectedUserInput)) ||
                String.IsNullOrEmpty(chatMessage.UserInput);

            if (isSourceMessageEmpty)
                return ParseResult.Failed;

            var parser = await GetParser(flowStep, chatParseField.ParseType, chatModel, chatMessage, searchMode);
            if (parser == null)
                return ParseResult.Failed;

            ParseResult result = await parser.ParseAsync(chatModel.CurrentState, chatParseField, chatMessage);
            parser.UpdateState(chatModel.CurrentState, flowStep, chatParseField, result);

            await SendParseEvent(chatMessage, chatModel, chatParseField, searchMode, result);

            return result;
        }

        private static async Task SendParseEvent(ChatMessage chatMessage, ChatModel chatModel, Chat_ParseField chatParseField, bool searchMode, ParseResult result)
        {
            if (kinesisService != null)
            {
                if ((chatParseField.ParseType == ChatRuleType.LossCategoryParser) || (chatParseField.ParseType == ChatRuleType.LossCategoryParserOptions))
                    await kinesisService.SendClassificationEvent(chatModel, chatParseField, result.Success, searchMode, "losscategory", chatMessage.LuisDamageClassifierOutput);
                else if ((chatParseField.ParseType == ChatRuleType.Parse) || (chatParseField.ParseType == ChatRuleType.IntentGatewayParser))
                {
                    await kinesisService.SendClassificationEvent(chatModel, chatParseField, result.Success, searchMode, chatMessage.Classifications);
                    LogClassificationResult(chatMessage.Classifications);
                }
                else if (!searchMode || result.Success)
                {
                    string answer;
                    var answerValue = chatModel.CurrentState.GlobalAnswers[chatParseField.FieldName];

                    if (answerValue is string)
                        answer = answerValue.ToString();
                    else
                        answer = JsonConvert.SerializeObject(answerValue);

                    await kinesisService.SendParseEvent(chatModel, chatParseField, result.Success, searchMode, answer);
                }
            }
        }

        private static void LogClassificationResult(ClassificationResults classifications)
        {
            var bestResult = classifications?.GetBestResult();
            if (bestResult != null)
                logger.InfoFormat("Parse: Message classified as '{0}' with score {1}", bestResult.Intent, bestResult.Probability);
        }

        async Task<ChatParserBase> GetParser(ChatFlowStep chatFlowStep, string parseType, ChatModel chatModel, ChatMessage chatMessage, bool searchMode)
        {
            switch (parseType)
            {
                case ChatRuleType.Parse:
                    return new IntentParser(classificationService, userDataFilterService, ChatConfiguration.MininimConfidenceRatio, chatModel.ContextRule?.DefaultClassifier);
                case ChatRuleType.IntentGatewayParser:
                    return new IntentGatewayParser(chatModel, scriptManager, classificationService, externalDataStorageService, chatConfiguration);
                case ChatRuleType.FuzzyMatch:
                    return new FuzzyMatchParser(chatModel, fuzzyMatchService, scriptManager, chatFlowStep);
                case ChatRuleType.AddressParser:
                    return new AddressParser(chatModel.ChatId, addressParseService);
                case ChatRuleType.NameParseNoHistory:
                case ChatRuleType.NameParse:
                    return new NameParser(textParserService);
                case ChatRuleType.ZipCodeParser:
                    return new ZipCodeParser();
                case ChatRuleType.Regex:
                    return new RegexParser();
                case ChatRuleType.ContinueParser:
                    if (searchMode || await IsUserBreakingOut(chatModel, chatMessage))
                        break;
                    return new ContinueParser();
                case ChatRuleType.PauseParser:
                    return new PauseParser();
                case ChatRuleType.DontKnowParser:
                    if (searchMode)
                        break;
                    return new DontKnowParser();
                case ChatRuleType.YesNoParser:
                    if (searchMode)
                        break;
                    return new YesNoParser();
                case ChatRuleType.PhoneNumberNoHistory:
                case ChatRuleType.PhoneNumber:
                    return new PhoneNumberParser(searchMode);
                case ChatRuleType.Email:
                    return new EmailParser(searchMode);
                case ChatRuleType.ParseDevice:
                    return new DeviceParser(deviceCatalog);
                case ChatRuleType.ParseBackupProvider:
                    return new BackupProviderParser();
                case ChatRuleType.ParseCarrier:
                    return new CarrierParser();
                case ChatRuleType.ParseRetailPartner:
                    return new RetailPartnerParser();
                case ChatRuleType.DateParser:
                    return new DateParser(1, luisDateParserService);
                case ChatRuleType.DateParserV2:
                    return new DateParser(2, luisDateParserService);
                case ChatRuleType.TimeParser:
                    return new TimeParser(luisDateParserService);
                case ChatRuleType.LossCategoryParserOptions:
                case ChatRuleType.LossCategoryParser:
                    return new LossCategoryParser(luisDamageClassifier);
                case ChatRuleType.AppNameParser:
                    return new AppNameParser();
                case ChatRuleType.BluetoothDeviceParser:
                    return new BluetoothDeviceParser();
                case ChatRuleType.ColorParser:
                    return new ColorParser(colorCatalog);
                case ChatRuleType.NumberParser:
                    return new NumberParser();
            }

            throw new ApplicationException($"Invalid parse type specified '{parseType}' on step {chatFlowStep.Flow}-{chatFlowStep.Id}");
        }

        async Task<bool> IsUserBreakingOut(ChatModel chatModel, ChatMessage chatMessage)
        {
            // Check for user breaking out with "ok" message if input message is longer
            // than simple continue message
            if (chatMessage.CorrectedUserInput.Length <= ChatConfiguration.MaximumSimpleContinueInputLength)
                return false;

            bool success = await ClassifyText(chatModel, chatMessage, null, TextClassificationService.CommonChatCategory, ChatConfiguration.MininimConfidenceRatio, false);
            if (success && (chatModel.CurrentState.LastClassification.GetBestResult().Intent == "commonchat-Breakout-Solution"))
                return true;

            return false;
        }

        void AddFieldToAutoFillList(ChatState chatState, string fieldName)
        {
            var value = chatState.GlobalAnswers[ChatStandardField.AutoFill] as string[];

            if (value == null)
            {
                chatState.GlobalAnswers[ChatStandardField.AutoFill] = new string[] { fieldName };
                return;
            }

            if (value.Contains(fieldName))
                return;

            var list = new List<string>(value)
            {
                fieldName
            };
            chatState.GlobalAnswers[ChatStandardField.AutoFill] = list.ToArray();
        }

        async Task ProcessActions(ChatModel chatModel, ChatFlowStep chatFlowStep)
        {

            await scriptManager.ProcessActions(chatModel, chatFlowStep, null, false);
        }

        bool CheckTooManyFails(ChatModel chatModel)
        {
            chatModel.IncreaseBadMessageCount();

            if ((chatModel.ConsecutiveBadMessageCount >= ChatConfiguration.MaximumConsecutiveBadMessages) ||
                (chatModel.BadMessageCount >= ChatConfiguration.MaximumTotalBadMessages))
                return true;

            return false;
        }

        async Task<ChatFlowStep> PushFailureStep(ChatModel chatModel, ChatFlowStep chatFlowStep, string failureFlowName)
        {
            if (CheckTooManyFails(chatModel))
                return await LoadStep("TransferAgent", "1", chatModel);

            chatModel.CurrentState.FlowStack.Push(new ChatStackItem(chatFlowStep, null, null));
            return await LoadStep(failureFlowName, "1", chatModel);
        }

        async Task<ChatFlowStep> LoadStep(string flowName, string id, ChatModel chatModel)
        {
            if ((flowName == "ContinueChat") && (id == "1"))
                chatModel.CurrentState.FlowPath.Clear();

            var step = await flowStepProvider.GetFlowStep(chatModel, flowName, id);

            if (step != null)
            {
                chatModel.CurrentState.AddToFlowPath(step.Flow);
                return step;
            }

            logger.ErrorFormat("Flow: Missing Flow.  Flow could not be found. {0}-{1}", flowName, id);
            return await flowStepProvider.GetMissingFlow(chatModel);
        }
        static bool AreAnyFieldsProcessed(Dictionary<Chat_ParseField, bool> processedFields)
        {
            if (processedFields == null)
                return false;

            if (processedFields.Any(x => x.Value == true))
                return true;

            return false;
        }

        async Task SendTagEvent(SessionData sessionData, string eventName, IDictionary<string, object> properties = null)
        {
            if (kinesisAnalyticsService != null)
                await kinesisAnalyticsService.SendTagEventAsync(sessionData, eventName.ToLower(), properties);
        }

        public static async Task<IChatScriptManager> CreateChatScriptManager(ChatModel chatModel, bool isNewChat, IExternalDataStorageService externalDataStorageService, IHubContext<NodeJSHub> hubContext, INodeServices nodeServices, ChatConfiguration configuration)
        {
            IChatScriptManager chatScriptManager;

            if (IsNodeManager(chatModel))
                chatScriptManager = new ChatScriptNodeManager(chatModel, externalDataStorageService, hubContext, nodeServices, configuration);
            else
                chatScriptManager = new ChatScriptManager(chatModel, externalDataStorageService, configuration);

            await chatScriptManager.Initialize();

            if (isNewChat)
                await chatScriptManager.StartChat(chatModel);
            
            return chatScriptManager;
        }

        static bool IsNodeManager(ChatModel chatModel)
        {
            return chatModel.UseNodeScripts;
        }
    }
}
