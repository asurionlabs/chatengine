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

using ChatWeb.Helpers;
using ChatWeb.Models;
using ChatWeb.Services;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace ChatWeb.Parsers
{
    public class IntentGatewayParser : ChatParserBase
    {
        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        readonly ChatModel chatModel;
        readonly IChatScriptManager chatScriptManager;
        readonly TextClassificationService classificationService;
        readonly IExternalDataStorageService externalDataStorageService;
        readonly ChatConfiguration chatConfiguration;

        public IntentGatewayParser(ChatModel chatModel, IChatScriptManager chatScriptManager, TextClassificationService classificationService, IExternalDataStorageService externalDataStorageService, ChatConfiguration chatConfiguration)
        {
            this.chatModel = chatModel;
            this.chatScriptManager = chatScriptManager;
            this.classificationService = classificationService;
            this.externalDataStorageService = externalDataStorageService;
            this.chatConfiguration = chatConfiguration;
        }

        public override async Task<ParseResult> ParseAsync(ChatState chatState, Chat_ParseField chatParseField, ChatMessage message)
        {
            var preFlowName = chatParseField.GetProperty("preFlowName");
            var preStepId = chatParseField.GetProperty("preStepId");
            var postFlowName = chatParseField.GetProperty("postFlowName");
            var postStepId = chatParseField.GetProperty("postStepId");

            if (String.IsNullOrEmpty(preFlowName) ||
                String.IsNullOrEmpty(preStepId) ||
                String.IsNullOrEmpty(postFlowName) ||
                String.IsNullOrEmpty(postStepId))
            {
                throw new ApplicationException("Parse: Missing rule data for IntentGateway Parser.");
            }

            message.Classifications = await ClassifyText(message.CorrectedUserInput, preFlowName, preStepId, postFlowName, postStepId, chatState.SessionData.IsSmsChannel);

            chatState.UpdateLastClassification(message.Classifications);

            if (!message.Classifications.IsSuccessful)
                return ParseResult.Failed;

            // We don't want common chat intent's here.  
            var intent = chatState.LastClassification.GetBestResult().Intent;
            if (intent != null && intent.StartsWith("commonchat-"))
                return ParseResult.Failed;

            return ParseResult.CreateSuccess(message.Classifications.GetBestResult().Result);
        }

        public async Task<ClassificationResults> ClassifyText(string text, string preFlowName, string preStepId, string postFlowName, string postStepId, bool allowSmsClassifications)
        {
            var setupFlowStep = await externalDataStorageService.GetFlowStep(preFlowName, preStepId, false);
            if (setupFlowStep == null)
                throw new ApplicationException($"Parse: Invalid Setup Flow step for IntentGateway parser. {preFlowName}-{preStepId}");

            var postFlowStep = await externalDataStorageService.GetFlowStep(postFlowName, postStepId, false);
            if (postFlowName == null)
                throw new ApplicationException($"Parse: Invalid Post Flow step for IntentGateway parser. {postFlowName}-{postStepId}");

            var classifierData = await chatScriptManager.ProcessActions(chatModel, setupFlowStep, null, true);

            var intentGatewayConfig = JsonConvert.DeserializeObject<IntentGatewayConfig>(JsonConvert.SerializeObject(classifierData));

            IntentGatewayService intentGatewayService;
            if (String.IsNullOrEmpty(intentGatewayConfig?.Url))
            {
                intentGatewayService = new IntentGatewayService(chatConfiguration.IntentGateway);
            }
            else
            {
                intentGatewayService = new IntentGatewayService(
                    new UrlConfig()
                    {
                        Url = intentGatewayConfig.Url,
                        Key = intentGatewayConfig.Key
                    });
                classifierData = intentGatewayConfig.Body;
            }

            var classifications = await classificationService.ClassifyAsync(intentGatewayService, 0.0, text, false, classifierData, allowSmsClassifications);

            await PostProcessResults(classifications, postFlowStep);

            return classifications;
        }

        private async Task PostProcessResults(ClassificationResults results, ChatFlowStep postFlowStep)
        {
            if (results.ClassifierResults == null)
                return;

            foreach (var classification in results.ClassifierResults)
            {
                if (classification.Source != "intentgateway")
                    continue;

                if (classification.RawResponse == null)
                    continue;

                object postProcessResult = await chatScriptManager.ProcessActions(chatModel, postFlowStep, JsonConvert.DeserializeObject<dynamic>(classification.RawResponse), true);
                if (postProcessResult == null)
                    continue;

                if (postProcessResult is string text)
                {
                    classification.Result = text;
                    classification.Intent = text;
                }
                else
                {
                    classification.Result = postProcessResult.FromScriptValue();

                    if (classification.Result is Dictionary<string, object> dictionaryResults)
                    {
                        if (dictionaryResults.ContainsKey("intent"))
                            classification.Intent = dictionaryResults["intent"] as string;
                        if (dictionaryResults.ContainsKey("score") && dictionaryResults["score"] is double score)
                            classification.Probability = score;
                    }
                }
            }
        }
    }
}