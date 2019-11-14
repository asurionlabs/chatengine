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

namespace ChatWeb.Parsers.Tests
{
    [TestClass]
    public class DeviceParserTests
    {
        readonly DeviceParser deviceParser;
        readonly DeviceCatalog deviceCatalog = new DeviceCatalog();

        public DeviceParserTests()
        {
            deviceCatalog.LoadDeviceListFromPath(@"..\..\..\..\ChatWeb\");
            deviceParser = new DeviceParser(deviceCatalog);
        }

        [TestMethod]
        public async Task TestDeviceMatcher()
        {
            string text = "my apple iphone 6s plis device";

            var result = await deviceParser.ParseAsync(null, null, new ChatMessage { UserInput = text });
            Assert.IsTrue(result.Success);
        }
    }
}
