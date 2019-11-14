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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatWeb.Services
{
    public class IntentGatewayService
    {
        readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        readonly RestApiService restApiIntentGateway;

        public IntentGatewayService(UrlConfig urlConfig)
        {
            this.restApiIntentGateway = new RestApiService(urlConfig);
        }

        public async Task<ClassificationResponse[]> Classify(object classifierData)
        {
            List<ClassificationResponse> results = new List<ClassificationResponse>();

            logger.DebugFormat("Network: IntentGateway calling '{0}' with '{1}'", restApiIntentGateway.BaseUrl, JsonConvert.SerializeObject(classifierData));
            var response = await restApiIntentGateway.CallRestApi(null, classifierData);

            if (response != null)
            {
                results.Add(new ClassificationResponse()
                {
                    Intent = "",
                    Probability = 1.0,
                    SourceData = JsonConvert.SerializeObject(classifierData),
                    Source = "intentgateway",
                    RawResponse = response
                });
            }

            return results.ToArray();
        }
    }
}
