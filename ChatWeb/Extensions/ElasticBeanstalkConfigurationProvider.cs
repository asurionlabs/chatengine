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

using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ChatWeb.Extensions
{
    public class ElasticBeanstalkConfigurationProvider : ConfigurationProvider
    {
        const string ElasticBeanstalkConfigPath = @"C:\Program Files\Amazon\ElasticBeanstalk\config\containerconfiguration";
        static readonly char[] JsonSplitChar = new char[] { '=' };

        public override void Load()
        {
            if (!File.Exists(ElasticBeanstalkConfigPath))
                return;

            var config = JObject.Parse(File.ReadAllText(ElasticBeanstalkConfigPath));
            var env = (JArray)config["iis"]["env"];

            if (env.Count == 0)
                return;

            foreach (var item in env.Select(i => (string)i))
            {
                string[] keypair = item.Split(JsonSplitChar, 2);
                Data[keypair[0]] = keypair[1];
            }
        }
    }

    public class ElasticBeanstalkConfigurationSource : IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new ElasticBeanstalkConfigurationProvider();
        }
    }

    public static class ElasticBeanstalkExtensions
    {
        public static IConfigurationBuilder AddElasticBeanstalk(this IConfigurationBuilder configurationBuilder)
        {
            var cb = configurationBuilder.Add(new ElasticBeanstalkConfigurationSource());
            return configurationBuilder;
        }
    }
}
