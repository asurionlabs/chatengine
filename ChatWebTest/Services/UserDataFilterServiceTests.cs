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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatWeb.Models;

namespace ChatWeb.Services.Tests
{
    [TestClass]
    public class UserDataFilterServiceTests
    {
        [TestMethod()]
        public void FilterUserDataTest()
        {
            UserDataFilterService filterService = new UserDataFilterService();

            string testText = "My phone number is 800-555-1212. email: me@here.com got it? name: My name is Mike.";

            string filtered = filterService.FilterUserData(null, testText, true);
            Assert.AreEqual(filtered, "My phone number is <PHONE>. email: <EMAIL>.com got it? name: My name is Mike.");

            filtered = filterService.FilterUserData(null, testText, false);
            Assert.AreEqual(filtered, "My phone number is . email: .com got it? name: My name is Mike.");
        }

        [TestMethod]
        public void FilterUserDataShortNameTest()
        {
            ChatState state = new ChatState();
            UserDataFilterService filterService = new UserDataFilterService();
            state.AddPIIText(ChatWeb.Models.PIIType.Low, "mary", "NAME");
            state.AddPIIText(ChatWeb.Models.PIIType.Low, "n", "NAME");

            string testText = "My name is mary n.";

            string filtered = filterService.FilterUserData(state, testText, true);
            Assert.AreEqual(filtered, "My name is <NAME> n.");
        }
    }
}