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

using ChatWeb;
using ChatWeb.Models;
using ChatWeb.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatWebTest
{
    public class ChatEngineFramework
    {
        ChatWeb.Services.ChatEngine chatEngine;

        public ChatEngineFramework()
        {
            Amazon.AWSConfigs.AWSRegion = "us-east-1";
            Amazon.AWSConfigs.AWSProfileName = "";

            ChatConfiguration chatConfiguration = new ChatConfiguration();
            AWSDynamoService dataService = new AWSDynamoService(chatConfiguration);
            FlowStepProvider flowStepProvider = new FlowStepProvider(dataService);

            chatEngine = new ChatWeb.Services.ChatEngine(null, chatConfiguration, null, dataService, null, null, null, null, null, null, null, null, flowStepProvider);
        }

        public async Task<ChatResponse> HandleRequest(ChatRequest request)
        {
            return await chatEngine.HandleRequest(request);
        }

        public async Task CheckRequest(ChatRequest request, ChatResponse expected, int step)
        {
            var actual = await chatEngine.HandleRequest(request);
            Assert.IsNull(actual.Error, "API Error triggered. {0}", actual.Error);
            MatchResponse(actual, expected, step);
        }

        void MatchResponse(ChatResponse actual, ChatResponse expected, int step)
        {
            // Compare Steps
            if (expected.Steps != null)
            {
                Assert.AreEqual(actual.Steps.Length, expected.Steps.Length, $"{step} - Step length different");

                for (int i = 0; i < actual.Steps.Length; i++)
                {
                    Assert.AreEqual(actual.Steps[i].FlowName, expected.Steps[i].FlowName, $"{step} - Step {i} different FlowName");
                    Assert.AreEqual(actual.Steps[i].StepId, expected.Steps[i].StepId, $"{step} - Step {i} different StepId");
                }
            }

            // Compare messages
            if (expected.Messages?.Length > 0)
            {
                Assert.AreEqual(actual.Messages?.Length, expected.Messages?.Length, $"{step} -  Message length different");

                for (int i = 0; i < expected.Messages.Length; i++)
                {
                    Assert.AreEqual(expected.Messages[i], actual.Messages[i], $"{step} - Message {i} different");
                }
            }

            Assert.AreEqual(expected.TransferToAgent, actual.TransferToAgent, $"{step} - TransferToAgent different");
        }

     }
}
