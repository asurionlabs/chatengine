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
using ChatWeb.Parsers;

namespace ChatWeb.Parsers.Tests
{
    [TestClass]
    public class NumberParserTests
    {
        public NumberParserTests()
        {
        }

        [TestMethod()]
        public async Task ParseTest()
        {
            var parser = new NumberParser();

            var result = await parser.ParseAsync(null,
                null,
                new ChatMessage { UserInput = "56 nutter butter" });
            Assert.AreEqual((int)result.Answer, 56);

            result = await parser.ParseAsync(null,
                null,
                new ChatMessage { UserInput = "nutter butter" });
            Assert.IsFalse(result.Success);

            result = await parser.ParseAsync(null,
                new ChatWeb.Models.Chat_ParseField
                {
                    RuleData = "{ \"AllowedValues\":\"32 64\" }"
                },
                new ChatMessage { UserInput = "56 nutter butter" });
            Assert.IsFalse(result.Success);

            result = await parser.ParseAsync(null,
                new ChatWeb.Models.Chat_ParseField
                {
                    RuleData = "{ \"AllowedValues\":\"32 64\" }"
                },
                new ChatMessage { UserInput = "64gigs" });
            Assert.AreEqual((int)result.Answer, 64);
        }

    }
}
