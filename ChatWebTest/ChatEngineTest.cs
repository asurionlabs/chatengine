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
using System.Collections.Generic;

namespace ChatWebTest
{
    [TestClass]
    public class ChatEngineTest
    {
        ChatEngineFramework framework = new ChatEngineFramework();

        public ChatEngineTest()
        {
        }

        [TestMethod]
        public async Task TestFTErrorHandlerRetry()
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
            request.UserInput = "ft-ErrorHandler-Retry";
            int step = 1;

            var expected = new ChatResponse
            {
                Steps = new ChatStepId[] {
                    new ChatStepId("ft-ErrorHandler-Retry", "1")
                },
                Messages = new string[]
                {
                    "Enter a number:"
                }
            };
            await framework.CheckRequest(request, expected, step++);

            request.UserInput = "a";
            expected = new ChatResponse
            {
                Steps = new ChatStepId[] {
                    new ChatStepId("ft-ErrorHandler-Retry", "1")
                }
            };
            await framework.CheckRequest(request, expected, step++);

        }

        [TestMethod]
        public async Task TestFTErrorHandlerJump()
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
            request.UserInput = "ft-ErrorHandler-Jump";
            int step = 1;

            var expected = new ChatResponse
            {
                Steps = new ChatStepId[] {
                    new ChatStepId("ft-ErrorHandler-Jump", "1")
                },
                Messages = new string[]
                {
                    "Enter a number:"
                }
            };
            await framework.CheckRequest(request, expected, step++);

            request.UserInput = "a";
            expected = new ChatResponse
            {
                Steps = new ChatStepId[] {
                    new ChatStepId("ft-ErrorHandler-Jump", "10001888"),
                    new ChatStepId("GetIntent-AsurionLabs-Test", "10000318")
                }
            };
            await framework.CheckRequest(request, expected, step++);

        }

        [TestMethod]
        public async Task TestFTErrorHandlerIgnore()
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
            request.UserInput = "ft-ErrorHandler-Ignore";
            int step = 1;

            var expected = new ChatResponse
            {
                Steps = new ChatStepId[] {
                    new ChatStepId("ft-ErrorHandler-Ignore", "1")
                },
                Messages = new string[]
                {
                    "Enter a number:"
                }
            };
            await framework.CheckRequest(request, expected, step++);

            request.UserInput = "a";
            expected = new ChatResponse
            {
                Steps = new ChatStepId[] {
                    new ChatStepId("ft-ErrorHandler-Ignore", "2"),
                    new ChatStepId("GetIntent-AsurionLabs-Test", "10000318")
                }
            };
            await framework.CheckRequest(request, expected, step++);

        }

        [TestMethod]
        public async Task TestFTErrorHandlerCallAndRetry()
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
            request.UserInput = "ft-ErrorHandler-CallAndRetry";
            int step = 1;

            var expected = new ChatResponse
            {
                Steps = new ChatStepId[] {
                    new ChatStepId("ft-ErrorHandler-CallAndRetry", "1")
                },
                Messages = new string[]
                {
                    "Enter a number:"
                }
            };
            await framework.CheckRequest(request, expected, step++);

            request.UserInput = "a";
            expected = new ChatResponse
            {
                Steps = new ChatStepId[] {
                    new ChatStepId("ft-ErrorHandler-CallAndRetry", "10001888"),
                    new ChatStepId("ft-ErrorHandler-CallAndRetry", "1")
                }
            };
            await framework.CheckRequest(request, expected, step++);

        }

        [TestMethod]
        public async Task TestFTErrorHandlerDelayedAnswerCallAndRetry()
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
            request.UserInput = "ft-ErrorHandler-DelayedAnswer-CallAndRetry";
            int step = 1;

            var expected = new ChatResponse
            {
                Steps = new ChatStepId[] {
                    new ChatStepId("ft-ErrorHandler-DelayedAnswer-CallAndRetry", "1")
                },
                Messages = new string[]
                {
                    "Phone?"
                }
            };
            await framework.CheckRequest(request, expected, step++);

            request.UserInput = "6505551212";
            expected = new ChatResponse
            {
                Steps = new ChatStepId[] {
                    new ChatStepId("ft-ErrorHandler-DelayedAnswer-CallAndRetry", "10000568")
                }
            };
            await framework.CheckRequest(request, expected, step++);

            request.UserInput = "me@here.com";
            expected = new ChatResponse
            {
                Steps = new ChatStepId[] {
                    new ChatStepId("ft-ErrorHandler-DelayedAnswer-CallAndRetry", "10002611")
                }
            };
            await framework.CheckRequest(request, expected, step++);

            request.UserInput = "6505551111";
            expected = new ChatResponse
            {
                Steps = new ChatStepId[] {
                    new ChatStepId("ft-ErrorHandler-DelayedAnswer-CallAndRetry", "10002254"),
                    new ChatStepId("ft-ErrorHandler-DelayedAnswer-CallAndRetry", "10002611")
                }
            };
            await framework.CheckRequest(request, expected, step++);

            request.UserInput = "1/1/2017";
            expected = new ChatResponse
            {
                Steps = new ChatStepId[] {
                    new ChatStepId("ft-ErrorHandler-DelayedAnswer-CallAndRetry", "10000569"),
                    new ChatStepId("GetIntent-AsurionLabs-Test", "10000318")
                },
                Messages = new string[]
                {
                    "Phone: 6505551111\nEmail: me@here.com\nDate: 1/1/2017",
                    "What flow would you like to test?"
                }
            };
            await framework.CheckRequest(request, expected, step++);
        }

        [TestMethod]
        public async Task TestFTTranscript()
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
            request.UserInput = "ft-Transcript";
            int step = 1;

            var expected = new ChatResponse
            {
                Steps = new ChatStepId[] {
                    new ChatStepId("ft-Transcript", "1")
                },
                Messages = new string[]
                {
                    "Say something."
                }
            };
            await framework.CheckRequest(request, expected, step++);

            request.UserInput = "something";
            expected = new ChatResponse
            {
                Messages = new string[]
                {
                    "Count: 4\nFirst:\nAva: Thank you for contacting Asurion Labs. What flow would you like to test?\nFull Transcript\nAva: Thank you for contacting Asurion Labs. What flow would you like to test?, User: ft-Transcript, Ava: Say something. and User: something",
                    "What flow would you like to test?"
                }
            };
            await framework.CheckRequest(request, expected, step++);

        }

        [TestMethod]
        public async Task TestFTFormatObjectMessage()
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
            request.UserInput = "ft-FormatObjectMessage";
            int step = 1;

            var expected = new ChatResponse
            {
                Steps = new ChatStepId[] {
                    new ChatStepId("ft-FormatObjectMessage", "1"),
                    new ChatStepId("ft-FormatObjectMessage", "2"),
                    new ChatStepId("GetIntent-AsurionLabs-Test", "10000318")
                },
                Messages = new string[]
                {
                    "field1Val",
                    "What flow would you like to test?"
                }
            };
            await framework.CheckRequest(request, expected, step++);
        }

        [TestMethod]
        public async Task TestFTTransferToAgent()
        {
            ChatRequest request = new ChatRequest()
            {
                Channel = "chatweb",
                Partner = "AsurionLabs",
                Context = "Test"
            };

            var actual = await framework.HandleRequest(request);
            Assert.IsNotNull(actual, "Initial response is null");
            Assert.IsNull(actual.Status.Error, "API Error triggered. {0}", actual.Status.Error);
            Assert.IsNotNull(actual.ChatId, "Initial response did not give Chat Id");

            request.ChatId = actual.ChatId;
            request.UserInput = "ft-TransferToAgent";
            request.ClientData = new ChatClientData()
            {
                EstimatedLiveAgentWaitTime = 1
            };
            int step = 1;

            var expected = new ChatResponse
            {
                Steps = new ChatStepId[] {
                    new ChatStepId("ft-TransferToAgent", "1"),
                    new ChatStepId("ft-TransferToAgent", "2")
                },
                Messages = new string[]
                {
                    "Sorry I couldn't help you."
                },
                TransferToAgent = true
            };
            await framework.CheckRequest(request, expected, step++);
        }


        [TestMethod]
        public async Task TestMBTest()
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
            request.UserInput = "battery drains fast";
            int step = 1;

            var expected = new ChatResponse
            {
                Steps = new ChatStepId[] {
                    new ChatStepId("GetIntent-AsurionLabs-Test", "1"),
                    new ChatStepId("GetIntent-AsurionLabs-Test", "10000318")
                }
            };
            await framework.CheckRequest(request, expected, step++);

            request.UserInput = "mb-test";
            expected = new ChatResponse
            {
                Steps = new ChatStepId[] {
                    new ChatStepId("mb-test", "1"),
                    new ChatStepId("mb-test", "10000399")
                }
            };
            await framework.CheckRequest(request, expected, step++);

        }
    }
}
