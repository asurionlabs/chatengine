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
using ChatWeb.Models;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ChatWeb.Services;
using System.Collections.Generic;
using System.Threading;
using ChatWeb;

namespace ChatWeb.Services.Tests
{
    [TestClass]
    public class MessageFormatterTests
    {
        [TestMethod]
        public void TestMessageFormatterLocal()
        {
            ChatState state = new ChatState();
            var local = state.GetFlowAnswers("sampleflow");
            local.FieldAnswers["test"] = "local test";

            string text = "Format '%local.test%'";

            var formattedText = MessageFormatter.FormatMessage(state, "sampleflow", text);
            Assert.AreEqual(formattedText, "Format 'local test'");
        }

        [TestMethod]
        public void TestMessageFormatterGlobal()
        {
            ChatState state = new ChatState();
            state.GlobalAnswers["test"] = "global test";

            string text = "Format '%s.test%'";

            var formattedText = MessageFormatter.FormatMessage(state, "sampleflow", text);
            Assert.AreEqual(formattedText, "Format 'global test'");
        }

        [TestMethod]
        public void TestMessageFormatterArrayDeref()
        {
            ChatState state = new ChatState();
            state.GlobalAnswers["test"] = new int[] { 5, 6, 7 };

            string text = "Format '%s.test[0]%'";

            var formattedText = MessageFormatter.FormatMessage(state, "sampleflow", text);

            Assert.AreEqual(formattedText, "Format '5'");
        }

        [TestMethod]
        public void TestMessageFormatterMultiLevel()
        {
            ChatState state = new ChatState();
            state.GlobalAnswers["test"] = new Dictionary<string, object>()
                {
                    { "l1", new Dictionary<string, object>()
                        {
                            { "l2/test", 25 }
                    }
                    }
                };

            string text = "Format '%s.test.l1[\"l2/test\"]%'";

            var formattedText = MessageFormatter.FormatMessage(state, "sampleflow", text);
            Assert.AreEqual(formattedText, "Format '25'");
        }

        [TestMethod]
        public void TestMessageFormatterMultiLevelError()
        {
            ChatState state = new ChatState();
            state.GlobalAnswers["test"] = new Dictionary<string, object>()
                {
                    { "l1", new Dictionary<string, object>()
                        {
                            { "l2/test", 25 }
                    }
                    }
                };

            string text = "Format '%s.test.nothere[\"l2/test\"]%'";

            var formattedText = MessageFormatter.FormatMessage(state, "sampleflow", text);
            Assert.AreEqual(formattedText, "Format '%s.test.nothere[\"l2/test\"]%'");
        }

        [TestMethod]
        public void TestMessageFormatterGlobalOldMethod()
        {
            ChatState state = new ChatState();
            // Note, variable starts with 's' to make sure the s is treated properly in formatting
            state.GlobalAnswers["session"] = "global test";

            string text = "Format '%session%'";

            var formattedText = MessageFormatter.FormatMessage(state, "sampleflow", text);
            Assert.AreEqual(formattedText, "Format 'global test'");
        }

        [TestMethod]
        public void TestMessageFormatterLocalAndGlobal()
        {
            ChatState state = new ChatState();
            var local = state.GetFlowAnswers("sampleflow");
            local.FieldAnswers["test"] = "local test";
            state.GlobalAnswers["test"] = "global test";

            string text = "Format '%s.test%' and '%local.test%'";

            var formattedText = MessageFormatter.FormatMessage(state, "sampleflow", text);
            Assert.AreEqual(formattedText, "Format 'global test' and 'local test'");
        }


        [TestMethod]
        public void TestMessageFormatterBracketVariables()
        {
            ChatState state = new ChatState();
            var local = state.GetFlowAnswers("sampleflow");
            local.FieldAnswers["test/local"] = "local test";
            state.GlobalAnswers["test/global"] = "global test";

            string text = "Format '%s[\"test/global\"]%' and '%local[\"test/local\"]%'";

            var formattedText = MessageFormatter.FormatMessage(state, "sampleflow", text);
            Assert.AreEqual(formattedText, "Format 'global test' and 'local test'");
        }

        [TestMethod]
        public void TestMessageFormatterStringArray()
        {
            ChatState state = new ChatState();
            state.GlobalAnswers["test"] = new string[] { "apple", "orange", "banana" };

            string text = "Format '%s.test%'";

            var formattedText = MessageFormatter.FormatMessage(state, "sampleflow", text);
            Assert.AreEqual(formattedText, "Format 'apple, orange or banana'");
        }

        [TestMethod]
        public void TestMessageFormatterStringArrayAnd()
        {
            ChatState state = new ChatState();
            state.GlobalAnswers["test"] = new string[] { "apple", "orange", "banana" };

            string text = "Format '%&s.test%'";

            var formattedText = MessageFormatter.FormatMessage(state, "sampleflow", text);
            Assert.AreEqual(formattedText, "Format 'apple, orange and banana'");
        }

    }
}
