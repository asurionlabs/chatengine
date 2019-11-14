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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatWeb.Services
{
    public class FlowStepProvider
    {
        readonly ConcurrentDictionary<string, DateTime> chatFlowStepCacheTime = new ConcurrentDictionary<string, DateTime>();
        readonly ConcurrentDictionary<string, Dictionary<string, ChatFlowStep>> chatFlowStepCache = new ConcurrentDictionary<string, Dictionary<string, ChatFlowStep>>();
        readonly IExternalDataStorageService externalDataStorageService;
        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        const int cacheTimeoutSeconds = 5;

        public FlowStepProvider(IExternalDataStorageService externalDataStorage)
        {
            this.externalDataStorageService = externalDataStorage;
        }

        /// <summary>
        /// Loads the "MissingFlow" flow
        /// </summary>
        /// <param name="chatModel">Chat Model used to process partner/context, channel</param>
        /// <returns>MissingFlow step</returns>
        public async Task<ChatFlowStep> GetMissingFlow(ChatModel chatModel)
        {
            return await GetFlowStep(chatModel, $"MissingFlow", "9999999");
        }

        /// <summary>
        /// Loads the flow step from external data, checking for flowname overrides specified by 
        /// partner, context, and channel.
        /// </summary>
        /// <param name="flowName">Name of flow</param>
        /// <param name="id">Id of flow step</param>
        /// <param name="chatModel">Chat Model used to process partner/context, channel</param>
        /// <returns>Requested Flow Step or null if flow step not found.</returns>
        public async Task<ChatFlowStep> GetFlowStep(ChatModel chatModel, string flowName, string id)
        {
            Task<Dictionary<string, ChatFlowStep>> voiceStepTask = null;
            Task<Dictionary<string, ChatFlowStep>> channelStepTask = null;
            Task<Dictionary<string, ChatFlowStep>> partnerStepTask = null;
            Task<Dictionary<string, ChatFlowStep>> defaultStepTask = CacheFlow(flowName, chatModel.UseDebugFlows);
            var channel = chatModel.CurrentState.SessionData.Channel;
            var partnerContext = chatModel.CurrentState.SessionData.PartnerContext;

            List<Task> tasks = new List<Task>
            {
                defaultStepTask
            };

            if (channel == "GoogleActions")
            {
                voiceStepTask = CacheFlow($"{flowName}-{partnerContext}-Voice", chatModel.UseDebugFlows);
                tasks.Add(voiceStepTask);
            }

            if (!String.IsNullOrEmpty(channel) && (!flowName.EndsWith(channel)) && channel != "chatweb")
            {
                channelStepTask = CacheFlow($"{flowName}-{partnerContext}-{channel}", chatModel.UseDebugFlows);
                tasks.Add(channelStepTask);
            }

            if (!flowName.EndsWith(partnerContext))
            {
                partnerStepTask = CacheFlow($"{flowName}-{partnerContext}", chatModel.UseDebugFlows);
                tasks.Add(partnerStepTask);
            }

            await Task.WhenAll(tasks);

            if (voiceStepTask?.Result != null && voiceStepTask.Result.ContainsKey(id))
                return voiceStepTask.Result[id];

            if (channelStepTask?.Result != null && channelStepTask.Result.ContainsKey(id))
                return channelStepTask.Result[id];

            if (partnerStepTask?.Result != null && partnerStepTask.Result.ContainsKey(id))
                return partnerStepTask.Result[id];

            if (defaultStepTask?.Result != null && defaultStepTask.Result.ContainsKey(id))
                return defaultStepTask.Result[id];

            return null;
        }

        private async Task<Dictionary<string, ChatFlowStep>> CacheFlow(string flowName, bool useDebugFlows)
        {
            if (!chatFlowStepCacheTime.TryGetValue(flowName, out DateTime cacheTime)
                || (DateTime.Now - cacheTime).Seconds > cacheTimeoutSeconds
                || !chatFlowStepCache.ContainsKey(flowName))
            {
                var flowSteps = await externalDataStorageService.GetFlowSteps(flowName, useDebugFlows);
                chatFlowStepCache[flowName] = flowSteps.ToDictionary(x => x.Id, x => x);
                chatFlowStepCacheTime[flowName] = DateTime.Now;
            }

            return chatFlowStepCache[flowName];
        }
    }
}
