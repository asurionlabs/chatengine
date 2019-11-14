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
using Microsoft.ClearScript.V8;
using ChatWeb.Helpers;
using System.Collections.Generic;
using System.Dynamic;

namespace ChatWeb.Helpers.Tests
{
    [TestClass]
    public class ScriptHelpersTests
    {
        public ScriptHelpersTests()
        {
        }

        [TestMethod]
        public void TestJsonConvertComplex()
        {
            var testJson = @"
{
                ""datetime_entities"": [], 
  ""entities"": {}, 
  ""intents"": {
    ""abc"": [
      [
        ""avaml"",
        ""rf_test_first_v0.1.1""
      ], 
      [
        ""avaml"",
        ""gbdt_test_first_v0.2.0""
      ]
    ]
  }, 
  ""verbose"": {
    ""avaml"": {
      ""gbdt_test_first_v0.2.0"": {
        ""best"": ""abc"", 
        ""scores"": {
          ""def"": ""0.0105981"", 
          ""abc"": ""0.893007"", 
        }
      }, 
      ""rf_test_first_v0.1.1"": {
        ""best"": ""abc"", 
        ""scores"": {
          ""def"": ""0.3"", 
          ""abc"": ""0.9"", 
        }
      }
    }
  }
}";

            string code = "function test() { if (s.intents['abc'][0][0] === 'avaml') return true;  return false; }  test();";

            var converted = TestConversion(testJson, code);
        }

        [TestMethod]
        public void TestJsonConvertEmptyArray()
        {
            string testJson = @"{""test"": [ ] }";
            string code = "s.test.length === 0";

            TestConversion(testJson, code);
        }

        [TestMethod]
        public void TestJsonConvertStringArray()
        {
            string testJson = @"{""test"": [ ""abc"", ""def"", ""ghi""] }";
            string code = "function test() { s.convert = s.test;  if (s.test[0] === 'abc') return true;  return false; }  test();";

            var converted = TestConversion(testJson, code);
            var array = (object[])converted["convert"];
            Assert.AreEqual(array[0], "abc");
        }

        [TestMethod]
        public void TestJsonConvertIntArray()
        {
            string testJson = @"{""test"": [ 5, 6, 7 ] }";
            string code = "function test() { s.convert = s.test;  if (s.test[0] === 5) return true;  return false; }  test();";

            var converted = TestConversion(testJson, code);
            var array = (object[])converted["convert"];
            Assert.AreEqual(array[0], 5);
        }

        [TestMethod]
        public void TestJsonConvertDoubleArray()
        {
            string testJson = @"{""test"": [ 5.1, 6.2, 7.3 ] }";
            string code = "function test() { s.convert = s.test;  if (s.test[0] === 5.1) return true;  return false; }  test();";

            var converted = TestConversion(testJson, code);
            var array = (object[])converted["convert"];
            Assert.AreEqual(array[0], 5.1);
        }

        [TestMethod]
        public void TestJsonConvertMixArray()
        {
            string testJson = @"{""test"": [ ""abc"", 5, 5.1] }";
            string code = "function test() { s.convert = s.test;  if (s.test[0] === 'abc') return true;  return false; }  test();";

            var converted = TestConversion(testJson, code);
            var array = (object[])converted["convert"];
            Assert.AreEqual(array[0], "abc");
            Assert.AreEqual(array[1], 5);
            Assert.AreEqual(array[2], 5.1);
        }

        [TestMethod]
        public void TestJsonConvertObjArray()
        {
            string testJson = @"{""test"": [ { ""a"" : ""test"" }, { ""a"" : ""test2""} ] }";
            string code = "function test() { s.convert = s.test; if (s.test[0].a  === 'test') return true;  return false; }  test();";

            var converted = TestConversion(testJson, code);
        }

        private static Dictionary<string, object> TestConversion(string testJson, string code)
        {
            V8Runtime v8Runtime = new V8Runtime();
            using (var context = v8Runtime.CreateScriptEngine())
            {
                var result = JsonConvert.DeserializeObject<dynamic>(testJson);
                var jsValues = ScriptHelpers.ToScriptValue(result, context);
                context.AddHostObject("s", jsValues);

                Assert.IsTrue((bool)context.Evaluate(code));

                return ConvertJsToNative(jsValues);
            }
        }

        private static Dictionary<string, object> ConvertJsToNative(dynamic values)
        {
            var fields = new Dictionary<string, object>();

            ExpandoObject eo = values;
            foreach (var item in eo)
            {
                if (item.Value == null)
                    fields[item.Key] = null;
                else
                {
                    fields[item.Key] = item.Value?.FromScriptValue();
                }
            }

            return fields;
        }
    }
}
