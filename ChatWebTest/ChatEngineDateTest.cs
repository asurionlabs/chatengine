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
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ChatWeb.Services;
using ChatWeb.Models;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ChatWeb;

namespace ChatWebTest
{
    [TestClass]
    public class ChatEngineDateTest
    {
        ChatEngineFramework framework = new ChatEngineFramework();

        public ChatEngineDateTest()
        {
        }

        [TestMethod]
        public async Task TestFTDate1()
        {
            ChatRequest request = new ChatRequest()
            {
                Channel = "chatweb",
                Partner = "AsurionLabs",
                Context = "Test",
                TimeZone = "-0400" // EDT
            };

            var actual = await framework.HandleRequest(request);
            Assert.IsNotNull(actual, "Initial response is null");
            Assert.IsNull(actual.Status.Error, "API Error triggered. {0}", actual.Status.Error);
            Assert.IsNotNull(actual.ChatId, "Initial response did not give Chat Id");

            request.ChatId = actual.ChatId;
            request.UserInput = "ft-date1";
            int step = 1;

            var expected = new ChatResponse
            {
                Steps = new ChatStepId[] {
                    new ChatStepId("ft-date1", "1"),
                    new ChatStepId("ft-date1", "10000399")
                },
                Messages = new string[]
                {
                    "FlowTest Date",
                    "Date?"
                }
            };
            await framework.CheckRequest(request, expected, step++);

            request.UserInput = "2/17/2017";
            expected = new ChatResponse
            {
                Steps = new ChatStepId[] {
                    new ChatStepId("ft-date1", "10000400"),
                    new ChatStepId("ft-date1", "10000405"),
                    new ChatStepId("ft-date1", "10001192"),
                    new ChatStepId("ft-date1", "10000406"),
                    new ChatStepId("GetIntent-AsurionLabs-Test", "10000318")
                }
            };
            await framework.CheckRequest(request, expected, step++);

        }

    }
}
