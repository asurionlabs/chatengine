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
    public class TextClassificationServiceTests
    {
        readonly TextClassificationService ClassificationService;
        readonly string ClassifierTestFile = "classifiertests.csv";


        public TextClassificationServiceTests()
        {
            var configPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\ChatWeb\azureml-dev.config"));
            var chatConfiguration = new ChatConfiguration();
            var awsDynamoService = new AWSDynamoService(chatConfiguration);

            ClassificationService = new TextClassificationService(configPath, awsDynamoService, null);
        }

        [TestMethod]
        public Task TestClassification()
        {
            return Task.CompletedTask;
        }


        /// <summary>
        /// This tests the engines ability to combine the results of multiple classifiers in the right way to get a correct answer.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TestClassificationResults()
        {

            var tests = File.ReadAllLines(ClassifierTestFile);
            var splitChar = new char[] { ',' };

            var answers = new List<string>();

            var tasks = tests.Select(test =>
            {
                if (String.IsNullOrEmpty(test))
                    return null;

                var fields = test.Split(splitChar, 2);
                if (fields.Length < 2)
                    return null;

                return TestMessage(fields[1], fields[0]);

                //answers.Add($"{classification},{fields[1]}");
            });

            var results = await Task.WhenAll(tasks);

            //File.WriteAllLines(ClassifierTestFile + "2.txt", answers.ToArray());
        }

        SemaphoreSlim m_lock = new SemaphoreSlim(10);

        async Task<string> TestMessage(string message, string expectedIntent)
        {
            await m_lock.WaitAsync();
            try
            {

                var response = await ClassificationService.ClassifyAsync(null, ChatConfiguration.MininimConfidenceRatio, message, true, false);
                //if (response?.Length == 0)
                //    return "";

                if (String.IsNullOrEmpty(expectedIntent))
                {
                    Assert.AreEqual(response?.TopResults?.Length, 0, $"Test: {message}");
                    return null;
                }

                Assert.AreNotEqual(response?.TopResults?.Length, 0, $"Test: {message}");
                Assert.AreEqual(expectedIntent, response.TopResults[0].Intent, $"Test: {message}");

                return response.TopResults[0].Intent;
            }
            finally
            {
                m_lock.Release();
            }
        }
    }
}
