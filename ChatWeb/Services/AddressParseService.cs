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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace ChatWeb.Services
{
    public class AddressParseService
    {
        readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        const int Timeout = 40000;
        RestApiService restApiService;

        public AddressParseService(ChatConfiguration chatConfiguration)
        {
            Dictionary<string, string> headers = null;

            if (!String.IsNullOrEmpty(chatConfiguration.AddressParse.Key))
            {
                headers = new Dictionary<string, string>()
                {
                    { "x-api-key", chatConfiguration.AddressParse.Key }
                };
            }

            restApiService = new RestApiService(chatConfiguration.AddressParse.Url, null, headers);
        }

        public async Task<ParseAddressDTO> Parse(string chatId, ChatState chatState, string text)
        {
            var request = new AddressParseRequest()
            {
                sessionId = chatId,
                interactionId = System.Diagnostics.Trace.CorrelationManager.ActivityId.ToString(),
                addressText = text,
                partner = chatState.GlobalAnswers.GetFieldAnswer(ChatStandardField.Partner)?.ToLower(),
                context = chatState.GlobalAnswers.GetFieldAnswer(ChatStandardField.Context)?.ToLower()
            };

            var response = await restApiService.CallRestApi<AddressParseResponse>(null, request, Timeout);

            if ((response?.parseAddressDTO?.response == "0") && (response?.parseAddressDTO?.addresses?.Length > 0))
                return response.parseAddressDTO;

            logger.InfoFormat("Parse: AddressParseService did not parse any address.");

            return null;
        }
    }
}