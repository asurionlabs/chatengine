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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ChatWeb.Services;
using ChatWeb.Models;
using System.Threading.Tasks;
using ChatWeb;
using System.IO;

namespace ChatWeb.Parsers.Tests
{
    [TestClass]
    public class ColorParserTests
    {
        readonly ColorCatalog colorCatalog = new ColorCatalog();
        readonly ColorParser colorParser;
        readonly ChatConfiguration chatConfiguration = new ChatConfiguration();

        public ColorParserTests()
        {
            colorCatalog.LoadCatalog(@"..\..\..\..\ChatWeb\colorlist.csv");
            colorParser = new ColorParser(colorCatalog);
        }

        [TestMethod]
        public async Task TestColorParser()
        {
            await TestColor("i have a red iphone", "Red");
            await TestColor("i have a blk iphone" , "Black");
            await TestColor("white", "White" );
            await TestColor("apple iphone 6s plus white 32GB", "White");
        }

        async Task TestColor(string text, string color)
        {
            var result = await colorParser.ParseAsync(null, null, new ChatMessage { UserInput = text });
            var colorResult = result.Answer as ColorResult;
            Assert.AreEqual(colorResult.Name, color);
        }
    }
}
