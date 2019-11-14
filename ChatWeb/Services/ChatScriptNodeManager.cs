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

using ChatEngine.Hubs;
using ChatWeb.Models;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Threading.Tasks;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Threading;
using ChatEngine.Models;
using ChatWeb.Helpers;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.Extensions.Configuration;

namespace ChatWeb.Services
{
    public class ChatScriptNodeManager : IChatScriptManager
    {
        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        Regex regExFunction = new Regex("[^a-zA-Z0-9$_]", RegexOptions.Compiled);

        readonly IHubContext<NodeJSHub> nodeJsHubContext;
        readonly ChatScriptHost ChatScriptHost;
        readonly IExternalDataStorageService DataService;
        const int ReplyTimeout = 60 * 30 * 1000;
        readonly INodeServices NodeServices;
        static readonly JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None
        };

        public ChatScriptNodeManager(ChatModel chatModel, IExternalDataStorageService externalDataStorageService, IHubContext<NodeJSHub> nodeJsHubContext, INodeServices nodeServices, ChatConfiguration configuration)
        {
            this.ChatScriptHost = new ChatScriptHost(chatModel, configuration);
            this.DataService = externalDataStorageService;
            this.nodeJsHubContext = nodeJsHubContext;
            NodeServices = nodeServices;
        }

        #region IDisposable Support
        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {

                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        public Task Initialize()
        {
            return Task.CompletedTask;
        }

        public async Task StartChat(ChatModel chatModel)
        {
            logger.Debug("Script: StartChat Start.");

            var sharedScriptSteps = await DataService.ReadSharedScripts(chatModel.CurrentState.SessionData.PartnerContext);

            await ExecuteNode(chatModel, "SharedScripts", "startChat", null);

            foreach (var sharedStep in sharedScriptSteps)
            {
                // Skip base SharedScripts for now, since they will be handled with require()
                if (sharedStep.Flow == "SharedScripts")
                    continue;

                var functionName = $"{sharedStep.ObjectType}{sharedStep.Id}_{FormatFunctionName(sharedStep.Name)}";

                await ExecuteNode(chatModel, sharedStep.Flow, functionName, null);
            }
        }

        public async Task<object> GetVariable(ChatModel chatModel, string flowName, string variableName)
        {
            var fixedVarName = FixGlobalVarName(variableName);
            var data = new { flowName, variables = new string[] { fixedVarName } };
            var results = await CallNode(chatModel.DebugUserId, chatModel.ChatId, "getVariable", null, data);

            if (UtilityMethods.IsNullOrEmpty(results))
                return null;

            if (results[0] is NodeVariableResponse varResponse && varResponse.Result.ContainsKey(fixedVarName))
                return varResponse.Result[fixedVarName];

            return null;
        }

        string FixGlobalVarName(string varName)
        {
            if (!varName.StartsWith("s."))
                return varName;

            return varName.Remove(0, 1).Insert(0, "global");
        }

        public async Task NotifyFlowEnd(ChatModel chatModel, ChatStackItem chatStackItem)
        {
            var data = new { childFlow = chatStackItem.Child.FlowName, parentFlow = chatStackItem.Step.Flow };
            await CallNode(chatModel.DebugUserId, chatModel.ChatId, "flowEnded", null, data);
        }

        public async Task<bool> IsConditionMet(ChatModel chatModel, ChatFlowStep chatFlowStep, Chat_ChildRef childRef)
        {
            if (String.IsNullOrEmpty(childRef.Condition))
                return true;

            logger.DebugFormat("Script: IsConditionMet Start. '{0}'", childRef.Condition);

            var functionName = String.Format($"cond{chatFlowStep.Id}_{childRef.Id}");

           var result = await ExecuteNode(chatModel, chatFlowStep.Flow, functionName, null);

            if (!UtilityMethods.IsNullOrEmpty(result) && result[0] is NodeConditionResponse conditionResponse)
                return conditionResponse.Result;

            return false;
        }

        public async Task<object> ProcessActions(ChatModel chatModel, ChatFlowStep chatFlowStep, object functionParams, bool expectResult)
        {
            ScriptHost.ClearProperties();

            if (chatFlowStep.Actions == null &&
                chatFlowStep.ObjectType != ChatObjectType.Message &&
                chatFlowStep.ObjectType != ChatObjectType.StaticMessage)
                return null;

            logger.DebugFormat("Script: ProcessActions Start - Remote NodeJS. {0}:{1}", chatFlowStep.Flow, chatFlowStep.Id);

            object result = null;
            var functionName = $"{chatFlowStep.ObjectType}{chatFlowStep.Id}_{FormatFunctionName(chatFlowStep.Name)}";

            var response = await ExecuteNode(chatModel, chatFlowStep.Flow, functionName, functionParams);

            if (!UtilityMethods.IsNullOrEmpty(response))
            {
                foreach (var item in response)
                {
                    switch (item)
                    {
                        case NodeAbortResponse abortResponse: ChatScriptHost.Abort(abortResponse.Result); break;
                        // TODO: Check if actionResponse is different meaning than nodeVariableResponse
                        case NodeActionResponse actionResponse: result = actionResponse.Result?.FromScriptValue(); break;
                        case NodeAddPiiTextResponse addPiiTextResponse:
                            {
                                ChatScriptHost.AddPiiText(addPiiTextResponse.Result.PiiType,
                                    addPiiTextResponse.Result.Text,
                                    addPiiTextResponse.Result.Mask);
                                break;
                            }
                        case NodeCallFlowResponse callFlowResponse: ChatScriptHost.CallFlow(callFlowResponse.Result.FlowName, callFlowResponse.Result.Params); break;
                        case NodeChangeContextResponse changeContextResponse: ChatScriptHost.ChangeContext(changeContextResponse.Result.Partner, changeContextResponse.Result.Context); break;
                        case NodeErrorResponse errorResponse: throw new ScriptException(errorResponse.Stack, chatFlowStep.Flow, chatFlowStep.Id);
                        case NodeIncreaseFailureCountResponse increaseFailureCountResponse: ChatScriptHost.IncreaseFailureCount(); break;
                        case NodeLogResponse logResponse: ChatScriptHost.LogInfoMessage(logResponse.Result); break;
                        case NodeMessageResponse messageResponse: ChatScriptHost.UI("", messageResponse.Result); break;
                        case NodeSendEmailResponse sendEmailResponse:
                            {
                                ChatScriptHost.SendEmail(sendEmailResponse.Result.From,
                                    sendEmailResponse.Result.Recipients,
                                    sendEmailResponse.Result.Subject,
                                    sendEmailResponse.Result.Body,
                                    sendEmailResponse.Result.IsBodyHtml);
                                break;
                            }
                        case NodeSendTagEventResponse sendTagEventResponse: ChatScriptHost.SendTagEvent(sendTagEventResponse.Result.TagName, sendTagEventResponse.Result.Properties); break;
                        case NodePutKinesisEventResponse putKinesisEventResponse:
                            {
                                ChatScriptHost.PutKinesisEvent(putKinesisEventResponse.Result.Arn,
                                putKinesisEventResponse.Result.StreamName,
                                putKinesisEventResponse.Result.StsRoleSessionName,
                                putKinesisEventResponse.Result.PartitionKey,
                                putKinesisEventResponse.Result.EventData);
                                break;
                            }
                        case NodeSetQuickRepliesResponse setQuickRepliesResponse: ChatScriptHost.SetQuickReplies(setQuickRepliesResponse.Result.Choices); break;
                        case NodeTransferToAgentResponse transferToAgentResponse: 
                            {
                                ChatScriptHost.TransferToAgent(transferToAgentResponse.Result.TransferInfo, transferToAgentResponse.Result.Skill);
                                break;
                            }
                        case NodeUiResponse uiResponse: ChatScriptHost.UI(uiResponse.Result.CustomMarkup, uiResponse.Result.PlainText); break;
                        case NodeVariableResponse variableResponse: result = variableResponse.Result; break;
                    }
                }
            }

            logger.DebugFormat("Script: ProcessActions End - Remote NodeJS. {0}:{1}", chatFlowStep.Flow, chatFlowStep.Id);

            return result;
        }

        string FormatFunctionName(string name)
        {
            if (name == null)
                return "";

            return regExFunction.Replace(name, "_");
        }

        async Task<INodeResponse[]> ExecuteNode(ChatModel chatModel, string flowName, string functionName, object functionParams)
        {
            var context = new
            {
                global = chatModel.CurrentState.GlobalAnswers.FieldAnswers,
                local = chatModel.CurrentState.GetFlowAnswers(flowName).FieldAnswers,
                parameters = functionParams
            };

            var functionData = new
            {
                flowName,
                blockName = functionName
            };

            return await CallNode(chatModel.DebugUserId, chatModel.ChatId, "invoke", context, functionData);
        }

        async Task<INodeResponse[]> CallNode(string debugUserId, string sessionId, string method, object context, object data)
        {
            if (String.IsNullOrEmpty(debugUserId))
                return await CallLocalNode(sessionId, method, context, data);
            else
                return await CallRemoteNode(debugUserId, sessionId, method, context, data);
        }

        async Task<INodeResponse[]> CallLocalNode(string sessionId, string method, object context, object data)
        {
            try
            {
                // Do our own serialization so we can control the JSON serializer settings
                var contextJson = JsonConvert.SerializeObject(context, jsonSerializerSettings);
                var dataJson = JsonConvert.SerializeObject(data, jsonSerializerSettings);

                var resultsJson = await NodeServices.InvokeExportAsync<string>("./ScriptHost/chatEngineLocal.js", method, sessionId, contextJson, dataJson);

                var results = JsonConvert.DeserializeObject<INodeResponse[]>(resultsJson, jsonSerializerSettings);
                return results;
            }
            catch (Exception ex)
            {
                return new INodeResponse[] { new NodeErrorResponse()
                {
                    Stack = ex.Message
                }};
            }
        }

        async Task<INodeResponse[]> CallRemoteNode(string debugUserId, string sessionId, string method, object context, object data)
        {
            var resultId = Guid.NewGuid().ToString("N");
            var connection = NodeJSHub.GetConnection(debugUserId);
            connection.Results[resultId] = new Subject<object>();

            try
            {
                using (var cs = new CancellationTokenSource(ReplyTimeout))
                {
                    ISubject<object> subject = connection.Results[resultId];
                    var waitTask = subject.FirstAsync().ToTask(cs.Token);

                    await nodeJsHubContext.Clients.Client(connection.ConnectionId).SendAsync(method, sessionId, context, resultId, data);

                    var result = JsonConvert.SerializeObject(await waitTask);
                    var resultObject = JsonConvert.DeserializeObject<INodeResponse[]>(result);

                    subject.OnCompleted();
                    connection.Results.TryRemove(resultId, out _);

                    return resultObject;
                }
            }
            catch (TaskCanceledException ex)
            {
                logger.ErrorFormat("Script: Node timeout waiting for reply. {0}", ex.Message);

            }
            catch (Exception ex)
            {
                logger.ErrorFormat("Script: Node script errory. {0}", ex.Message);
            }

            return null;
        }

        public async Task<ErrorHandlerResult> ProcessErrorHandler(ChatModel chatModel, ChatFlowStep chatFlowStep)
        {
            logger.DebugFormat("Script: ProcessErrorHandler Start - Remote NodeJS. {0}:{1}", chatFlowStep.Flow, chatFlowStep.Id);

            var functionName = String.Format($"ErrorHandler{chatFlowStep.Id}_{FormatFunctionName(chatFlowStep.Name)}");

            var result = await ExecuteNode(chatModel, chatFlowStep.Flow, functionName, null);
            logger.DebugFormat("Script: ProcessErrorHandler End - Remote NodeJS. {0}:{1}", chatFlowStep.Flow, chatFlowStep.Id);

            return null;
        }

        public ChatScriptHost ScriptHost
        {
            get { return ChatScriptHost; }
        }

    }
}
