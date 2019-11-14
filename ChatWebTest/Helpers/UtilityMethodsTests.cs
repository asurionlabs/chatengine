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
using ChatWeb.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatWeb.Helpers.Tests
{
    [TestClass()]
    public class UtilityMethodsTests
    {
        struct TestValue
        {
        }

        [TestMethod()]
        public void IsValueTypeTest()
        {
            Assert.IsFalse(UtilityMethods.IsValueType(null));
            Assert.IsFalse(UtilityMethods.IsValueType(new object()));
            Assert.IsTrue(UtilityMethods.IsValueType(new TestValue()));
            Assert.IsTrue(UtilityMethods.IsValueType(5));
            Assert.IsFalse(UtilityMethods.IsValueType("abc"));
        }

        [TestMethod()]
        public void IsNullOrEmptyTest()
        {
            string[] nullArray = null;
            Assert.IsTrue(UtilityMethods.IsNullOrEmpty(nullArray));
            Assert.IsTrue(UtilityMethods.IsNullOrEmpty(new string[] { }));
            Assert.IsFalse(UtilityMethods.IsNullOrEmpty(new string[] { "abc" }));
        }


        [TestMethod()]
        public void ParseJavascriptBooleanTest()
        {
            Assert.IsTrue(UtilityMethods.ParseJavascriptBoolean(true));
            Assert.IsFalse(UtilityMethods.ParseJavascriptBoolean(false));
            Assert.IsTrue(UtilityMethods.ParseJavascriptBoolean("abc"));
            Assert.IsFalse(UtilityMethods.ParseJavascriptBoolean(""));
            Assert.IsTrue(UtilityMethods.ParseJavascriptBoolean(5));
            Assert.IsFalse(UtilityMethods.ParseJavascriptBoolean(0));
            Assert.IsFalse(UtilityMethods.ParseJavascriptBoolean(null));
            Assert.IsTrue(UtilityMethods.ParseJavascriptBoolean(new { test = "test" }));
        }
    }
}