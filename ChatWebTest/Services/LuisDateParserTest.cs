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

using ChatWeb.Models;
using ChatWeb.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatWeb.Services.Tests
{
    [TestClass]
    public class LuisDateParserTests
    {
        // Luis service evaluates text and returns an Entity with information about the date.
        // We use a parser to convert Luis output into an actual date based on our context.

        readonly DateTime baselineNow;

        public LuisDateParserTests()
        {
            baselineNow = new DateTime(2018, 7, 11, 5, 0, 0, DateTimeKind.Utc);
        }

        [TestMethod]
        public void TestLuisDates()
        {
            var testText = File.ReadAllText("LuisDateTest.json");
            var testData = JsonConvert.DeserializeObject<dynamic>(testText);
            int testCount = 0;

            foreach (var test in testData.tests)
            {
                testCount++;
                Console.WriteLine($"Testing {test.input} with Future:{test.assumeFuture}");

                var temp = JsonConvert.SerializeObject(test.entities);
                var entities = JsonConvert.DeserializeObject<LuisEntity[]>(temp);

                LuisDateParser parser = new LuisDateParser((DateTime)testData.now, 0);
                DateTimeParseResult[] result = parser.ParseLuisDates(entities, (bool)test.assumeFuture);

                Assert.AreEqual(test.expectedDates.Count, result.Length, (string)test.input);

                for (int i = 0; i < test.expectedDates.Count; i++)
                {
                    Assert.AreEqual((DateTime?)test.expectedDates[i].dateTime, result[i].DateTime, (string)test.input);
                    Assert.AreEqual((DateTime?)test.expectedDates[i].endDateTime, result[i].EndDateTime, (string)test.input);

                    if ((TimeSpan?)test.expectedDates[i].time == null)
                        Assert.IsNull(result[i].Time);
                    else
                        Assert.AreEqual(new Time(((TimeSpan?)test.expectedDates[i].time).Value), result[i].Time, (string)test.input);

                    if ((TimeSpan?)test.expectedDates[i].endTime == null)
                        Assert.IsNull(result[i].EndTime);
                    else
                        Assert.AreEqual(new Time(((TimeSpan?)test.expectedDates[i].endTime).Value), result[i].EndTime, (string)test.input);

                    Assert.AreEqual((string)test.expectedDates[i].modifier, result[i].Modifier, (string)test.input);
                }
            }

            // Advanced!
            // this week 5-9pm and next week 6-8pm

            Console.WriteLine($"Ran {testCount} date/time tests");
        }
    }
}
