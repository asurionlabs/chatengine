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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using ChatWeb.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatWeb.Models;
using System.IO;

namespace ChatWeb.Services.Tests
{
    [TestClass()]
    public class ChatScriptManagerTests
    {
        readonly ChatScriptManager scriptManager;

        public ChatScriptManagerTests()
        {
            var chatModel = new ChatModel();
            ChatConfiguration config = new ChatConfiguration();
            AWSDynamoService awsDynamoService = new AWSDynamoService(config);
            FlowStepProvider flowStepProvider = new FlowStepProvider(awsDynamoService);

            var chatEngine = new ChatEngine(null, config, null, awsDynamoService, null, null, null, null, null, null, null, null, flowStepProvider);
            scriptManager = new ChatScriptManager(chatModel, awsDynamoService, config);
            scriptManager.Initialize().Wait();
        }

        [TestMethod()]
        public async Task CheckEmptyArray()
        {
            ChatModel chatModel = new ChatModel();
            ChatFlowStep chatFlowStep = new ChatFlowStep() { Actions = new List<string> { "s.AAA = JSON.parse('[]');" } };

            await scriptManager.ProcessActions(chatModel, chatFlowStep, null, false);
            Assert.IsInstanceOfType(chatModel.CurrentState.GlobalAnswers.FieldAnswers["AAA"], typeof(object[]));
        }

        [TestMethod()]
        public async Task CheckEmptyObject()
        {
            ChatModel chatModel = new ChatModel();
            ChatFlowStep chatFlowStep = new ChatFlowStep() { Actions = new List<string> { "s.AAA = JSON.parse('{}');" } };

            await scriptManager.ProcessActions(chatModel, chatFlowStep, null, false);
            Assert.IsInstanceOfType(chatModel.CurrentState.GlobalAnswers.FieldAnswers["AAA"], typeof(object));
        }

        [TestMethod()]
        public async Task CheckObject()
        {
            ChatModel chatModel = new ChatModel();
            ChatFlowStep chatFlowStep = new ChatFlowStep() { Actions = new List<string> { "s.AAA = JSON.parse('{ \"abc\" : 5 }');" } };

            await scriptManager.ProcessActions(chatModel, chatFlowStep, null, false);

            Assert.IsInstanceOfType(chatModel.CurrentState.GlobalAnswers.FieldAnswers["AAA"], typeof(Dictionary<string, object>));
            Dictionary<string, object> aaa = chatModel.CurrentState.GlobalAnswers.FieldAnswers["AAA"] as Dictionary<string, object>;

            Assert.AreEqual(aaa.Keys.Count, 1);
            Assert.AreEqual(aaa["abc"], 5);
        }
    }
}