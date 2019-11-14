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

using Microsoft.ClearScript.V8;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ChatWeb.Extensions;
using ChatWeb.Helpers;
using System.Dynamic;
using System.Threading.Tasks;
using ChatWeb.Models;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace ChatWeb.Services
{
    public class ChatScriptManager : IDisposable, IChatScriptManager
    {
        static readonly string[] ControlledVariables = { "Partner", "Context", "SessionData", "ClientData" };
        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        readonly ChatModel ChatModel;
        readonly V8Runtime v8Runtime = new V8Runtime();
        readonly IExternalDataStorageService DataService;
        ChatScript[] SharedScripts;
        bool initialized;

        public ChatScriptManager(ChatModel chatModel, IExternalDataStorageService externalDataStorageService, ChatConfiguration configuration)
        {
            ScriptHost = new ChatScriptHost(chatModel, configuration);
            DataService = externalDataStorageService;
            ChatModel = chatModel;
        }

#region IDisposable Support
        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (v8Runtime != null)
                        v8Runtime.Dispose();

                    if (ScriptHost != null)
                        ScriptHost.Dispose();
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
#endregion

        public async Task Initialize()
        {
            var sharedSteps = await DataService.ReadSharedScripts(ChatModel.CurrentState.SessionData.PartnerContext);

            SharedScripts = (from step in sharedSteps
                    where step.Actions != null
                    from action in step.Actions
                    select new ChatScript { Name = step.Name, Content = action }).ToArray();

            PrecompileScripts();

            initialized = true;
        }

        public Task StartChat(ChatModel chatModel)
        {
            return Task.CompletedTask;
        }

        public ChatScriptHost ScriptHost { get; }

        private void PrecompileScripts()
        {
            using (var engine = v8Runtime.CreateScriptEngine())
            {
                LoadSharedScripts(engine, true);
            }
        }

        public Task<object> GetVariable(ChatModel chatModel, string flowName, string variableName)
        {
            if (!initialized)
                throw new ApplicationException("ChatScriptManager is not initialized.  Call Initialize()");

            return Task.FromResult(MessageFormatter.ResolveVariable(chatModel.CurrentState, flowName, variableName));
        }

        public Task NotifyFlowEnd(ChatModel chatModel, ChatStackItem chatStackItem)
        {
            var result = chatModel.CurrentState.GetFlowAnswers(chatStackItem.Child.FlowName).GetFieldAnswer<object>("result");
            var callerAnswers = chatModel.CurrentState.GetFlowAnswers(chatStackItem.Step.Flow);
            foreach (var key in callerAnswers.FieldAnswers.Keys.ToArray())
            {
                if (callerAnswers[key] is FlowResult flowResult)
                {
                    if (flowResult.ResultId == chatStackItem.ResultPlaceholder.ResultId)
                        callerAnswers[key] = result;
                }
            }

            return Task.CompletedTask;
        }

        public Task<bool> IsConditionMet(ChatModel chatModel, ChatFlowStep chatFlowStep, Chat_ChildRef childRef)
        {
            if (!initialized)
                throw new ApplicationException("ChatScriptManager is not initialized.  Call Initialize()");

            if (String.IsNullOrEmpty(childRef.Condition))
                return Task.FromResult(true);

            ChatVariables globalFields = chatModel.CurrentState.GlobalAnswers;
            ChatVariables flowFields = chatModel.CurrentState.GetFlowAnswers(chatFlowStep.Flow);

            if (AwsUtilityMethods.IsRunningOnAWS)
                AWSXRayRecorder.Instance.BeginSubsegment("Script: Evaluate Condition");
            try
            {
                //logger.DebugFormat("Script: IsConditionMet Start. '{0}'", childRef.Condition);

                return Task.FromResult(
                    RunScript<bool>((engine, globalValues, flowValues) =>
                    {
                        var result = engine.Evaluate(childRef.Condition);

                        if (!(result is bool))
                            logger.WarnFormat("Script: IsCondition did not return true/false.  Evaluating with Javascript rules. '{0}', result: '{1}'", childRef.Condition, result);

                        return UtilityMethods.ParseJavascriptBoolean(result);

                    }, globalFields, flowFields, null)
                );
            }
            catch (Exception ex)
            {
                if (AwsUtilityMethods.IsRunningOnAWS)
                    AWSXRayRecorder.Instance.AddException(ex);
                logger.ErrorFormat("Script: IsConditionMet Error. '{0}' '{1}'", childRef.Condition, ex.Message);
            }
            finally
            {
                if (AwsUtilityMethods.IsRunningOnAWS)
                    AWSXRayRecorder.Instance.EndSubsegment();
            }

            return Task.FromResult(false);
        }

        public Task<object> ProcessActions(ChatModel chatModel, ChatFlowStep chatFlowStep, object functionParams, bool expectResult)
        {
            if (!initialized)
                throw new ApplicationException("ChatScriptManager is not initialized.  Call Initialize()");

            ChatVariables globalFields = chatModel.CurrentState.GlobalAnswers;
            ChatVariables flowFields = chatModel.CurrentState.GetFlowAnswers(chatFlowStep.Flow);

            ScriptHost.ClearProperties();

            if (chatFlowStep.Actions == null)
                return Task.FromResult<object>(null);

            //logger.DebugFormat("Script: ProcessActions Start JScriptEngine. {0}:{1}", chatFlowStep.Flow, chatFlowStep.Id);

            object result = null;

            if (AwsUtilityMethods.IsRunningOnAWS)
                AWSXRayRecorder.Instance.BeginSubsegment("Script: ProcessActions");
            try
            {
                return Task.FromResult(
                    RunScript<object>((engine, globalValues, flowValues) =>
                    {
                        // Performance optimization for ClearScript.  
                        // If we dont want the result, we dont need to marshal the result back.
                        chatFlowStep.Actions.ForEach(action => result = RunAction(engine, action, expectResult));

                        ConvertFieldsToNative(globalFields, globalValues, true);
                        ConvertFieldsToNative(flowFields, flowValues, false);
                        return result;
                    }, globalFields, flowFields, functionParams)
                );
            }
            catch (Exception ex)
            {
                if (AwsUtilityMethods.IsRunningOnAWS)
                    AWSXRayRecorder.Instance.AddException(ex);

                if (ex is Microsoft.ClearScript.ScriptEngineException scriptEx)
                {
                    logger.ErrorFormat("Script: ProcessActions Error. '{0}'", scriptEx.ErrorDetails);
                    throw new ScriptException(scriptEx.ErrorDetails, ex, chatFlowStep.Flow, chatFlowStep.Id);
                }

                logger.ErrorFormat("Script: ProcessActions Error. '{0}'", ex.Message);
                throw;
            }
            finally
            {
                if (AwsUtilityMethods.IsRunningOnAWS)
                    AWSXRayRecorder.Instance.EndSubsegment();
            }
        }

        static object RunAction(V8ScriptEngine engine, string action, bool expectResult)
        {

            action = String.Format("function __temp(parameters) {{\n{0}\n}}\nlet __tmpArgs = args.param1;\ntry {{ if (__tmpArgs) {{ __tmpArgs = JSON.parse(__tmpArgs); }} }} catch (e) {{ }}\n__temp(__tmpArgs);", action);
            if (expectResult)
                return engine.Evaluate(action)?.FromScriptValue();

            engine.Execute(action);
            return null;
        }

        public Task<ErrorHandlerResult> ProcessErrorHandler(ChatModel chatModel, ChatFlowStep chatFlowStep)
        {
            if (!initialized)
                throw new ApplicationException("ChatScriptManager is not initialized.  Call Initialize()");

            string script = chatFlowStep.ErrorHandler;
            ChatVariables globalFields = chatModel.CurrentState.GlobalAnswers;
            ChatVariables flowFields = chatModel.CurrentState.GetFlowAnswers(chatFlowStep.Flow);

            if (AwsUtilityMethods.IsRunningOnAWS)
                AWSXRayRecorder.Instance.BeginSubsegment("Script: Process Parser Error Handler");
            try
            {

                return Task.FromResult(
                    RunScript<ErrorHandlerResult>((engine, globalValues, flowValues) =>
                    {
                        dynamic result = engine.Evaluate(script);
                        if (result == null)
                            return null;

                        ConvertFieldsToNative(globalFields, globalValues, true);
                        ConvertFieldsToNative(flowFields, flowValues, false);

                        return ScriptHelpers.ConvertToType<ErrorHandlerResult>(result);
                    }, globalFields, flowFields, null)
                );
            }
            catch (Exception ex)
            {
                if (AwsUtilityMethods.IsRunningOnAWS)
                    AWSXRayRecorder.Instance.AddException(ex);
                logger.ErrorFormat("Script: Process Parser Error Handler Error. '{0}'", ex.Message);
                throw;
            }
            finally
            {
                if (AwsUtilityMethods.IsRunningOnAWS)
                    AWSXRayRecorder.Instance.EndSubsegment();
            }
        }


        private TResult RunScript<TResult>(Func<V8ScriptEngine, dynamic, dynamic, TResult> function, ChatVariables globalFields, ChatVariables flowFields, object functionParams)
        {
            using (var context = v8Runtime.CreateScriptEngine())
            {
                dynamic globalValues = globalFields.FieldAnswers.ToDynamic(context);
                dynamic flowValues = flowFields.FieldAnswers.ToDynamic(context);

                ScriptHost.ScriptContext = context;

                ConfigureScriptEngine(context, globalValues, flowValues, functionParams);

                TResult result = function(context, globalValues, flowValues);
                //if (logger.IsDebugEnabled)
                //    logger.DebugFormat("Script: Stats: {0}", JsonConvert.SerializeObject(context.GetRuntimeHeapInfo()));

                return result;
            }
        }

        private void ConfigureScriptEngine(V8ScriptEngine context, dynamic globalValues, dynamic flowValues, object functionValues)
        {
            // Always serialize object to string, and let the JS side deserialize it to proper JScript objects
            dynamic valueProxy = null;
            if (functionValues == null)
                valueProxy = new object();
            else
                valueProxy = new { param1 = JsonConvert.SerializeObject(functionValues) };

            // Dummy objects to support scripts meant for NodeJS
            var exports = new Dictionary<string, object>().ToDynamic(context);
            context.AddHostObject("exports", exports);
            context.AddHostObject("module", new { exports });

            context.AddHostObject("chatContext", new
            {
                isNode = false,
                api = ScriptHost,
                global = globalValues,
                local = flowValues,
                parameters = valueProxy,
                sessionId = ChatModel.ChatId
            });

            // Add backward compatibility
            context.AddHostObject("args", valueProxy);
            context.AddHostObject("local", flowValues);
            context.AddHostObject("global", globalValues);
            context.AddHostObject("s", globalValues);
            context.AddHostObject("api", ScriptHost);

            LoadSharedScripts(context, false);
        }

        private void LoadSharedScripts(V8ScriptEngine context, bool precompileOnly)
        {
            if (SharedScripts == null)
                return;

            foreach (var script in SharedScripts)
            {
                try
                {
                    if (precompileOnly)
                    {
                        context.Compile(script.Content, V8CacheKind.Code, out byte[] cache);
                        script.CompiledCache = cache;
                    }
                    else
                    {
                        V8Script v8Script = context.Compile(script.Content, V8CacheKind.Code, script.CompiledCache, out bool accepted);
                        context.Execute(v8Script);
                    }
                }
                catch (Exception ex)
                {
                    logger.ErrorFormat("Script: Shared script failed to run. {0}.  {1}", script.Name, ex.Message);
                    throw;
                }
            }
        }

        private void ConvertFieldsToNative(ChatVariables fields, dynamic values, bool global)
        {
            ExpandoObject eo = values;
            foreach (var item in eo)
            {
                if (global && IsVariableControlled(item.Key))
                    continue;

                fields[item.Key] = item.Value?.FromScriptValue();
            }
        }

        private static bool IsVariableControlled(string variableName)
        {
            return ControlledVariables.Contains(variableName);
        }
    }
}