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

using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ChatWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;

namespace ChatWeb.Services
{
    public class AWSDynamoService : IDisposable, IExternalDataStorageService
    {
        DynamoDBContext context;
        AmazonDynamoDBClient client;
        bool disposed;
        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        const string SharedStepsFlowName = "SharedScripts";
        ChatConfiguration chatConfiguration;

        public AWSDynamoService(ChatConfiguration chatConfiguration)
        {
            this.chatConfiguration = chatConfiguration;

            logger.Debug($"Internal: Creating {nameof(AWSDynamoService)}");
            client = new AmazonDynamoDBClient();

#if !XRAY2
            string whitelistPath = System.Web.Hosting.HostingEnvironment.MapPath("/AWSWhitelist.json");

            var tracer = new Amazon.XRay.Recorder.Handlers.AwsSdk.AWSSdkTracingHandler(Amazon.XRay.Recorder.Core.AWSXRayRecorder.Instance, whitelistPath);
            tracer.AddEventHandler(client);
#endif

            DynamoDBContextConfig config = new DynamoDBContextConfig()
            {
                TableNamePrefix = chatConfiguration.AwsTablePrefix
            };
            context = new DynamoDBContext(client, config);
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

                    if (context != null)
                    {
                        context.Dispose();
                        context = null;
                    }

                    if (client != null)
                    {
                        client.Dispose();
                        client = null;
                    }
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }
            disposed = true;
        }

        public static string ToDynamoDateString(DateTime date)
        {
            return date.ToString("yyyy-MM-ddTHH:mm:ss.fffK");
        }

        public async Task<List<ChatFlowStep>> GetFlowSteps(string flowName, bool debugMode)
        {
            if (disposed)
                throw new ObjectDisposedException("AWSDynamoService");

            var config = new DynamoDBOperationConfig();
            if (debugMode)
                config.TableNamePrefix = chatConfiguration.DebugTablePrefix;

            var search = context.QueryAsync<ChatFlowStep>(flowName, config);
            var result = await search.GetRemainingAsync();

            if ((result.Count == 0) && (debugMode))
            {
                search = context.QueryAsync<ChatFlowStep>(flowName, config);
                result = await search.GetRemainingAsync();
            }

            return result;
        }

        public async Task<ChatFlowStep> GetFlowStep(string flowName, string stepId, bool debugMode)
        {
            if (disposed)
                throw new ObjectDisposedException("AWSDynamoService");
            
            var config = new DynamoDBOperationConfig();
            if (debugMode)
                config.TableNamePrefix = chatConfiguration.DebugTablePrefix;

            var step = await context.LoadAsync<ChatFlowStep>(flowName, stepId, config);

            if ((step == null) && (debugMode))
                step = await context.LoadAsync<ChatFlowStep>(flowName, stepId, null);

            return step;
        }

        public async Task<ChatReasonCategoryMap> GetChatReasonCategoryMap(string category)
        {
            if (disposed)
                throw new ObjectDisposedException("AWSDynamoService");

            return await context.LoadAsync<ChatReasonCategoryMap>(category);

        }

        public async Task<ContextRule> GetContextRule(string contextName)
        {
            if (disposed)
                throw new ObjectDisposedException("AWSDynamoService");

            return await context.LoadAsync<ContextRule>(contextName);
        }

        public async Task<ChatFlowStep[]> ReadSharedScripts(string partnerContext)
        {
            string[] flowNames = new string[] { SharedStepsFlowName, $"{SharedStepsFlowName}-{partnerContext}" };

            IEnumerable<Task<List<ChatFlowStep>>> allTasks = flowNames.Select(flowName => GetFlowSteps(flowName, false));
            IEnumerable<ChatFlowStep>[] allResults = await Task.WhenAll(allTasks);

            return allResults.SelectMany(x => x).ToArray();
        }
    }
}