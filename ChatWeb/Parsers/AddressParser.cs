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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using ChatWeb.Models;
using ChatWeb.Services;

namespace ChatWeb.Parsers
{
    public class AddressParser : ChatParserBase
    {
        readonly string chatId;
        readonly AddressParseService addressParseService;

        public AddressParser(string chatId, AddressParseService addressParseService)
        {
            this.chatId = chatId;
            this.addressParseService = addressParseService;
            this.PIIMask = "ADDRESS";
            this.PIIType = PIIType.Low;
        }

        public override async Task<ParseResult> ParseAsync(ChatState state, Chat_ParseField chatParseField, ChatMessage message)
        {
            var output = await addressParseService.Parse(chatId, state, GetTextSource(chatParseField, message));
            if (output?.addresses?.Length > 0)
                return ParseResult.CreateSuccess(output.addresses);

            return ParseResult.Failed;
        }

       public override void UpdateState(ChatState state, ChatFlowStep flowStep, Chat_ParseField chatParseField, ParseResult result)
        {
            base.UpdateState(state, flowStep, chatParseField, result);

            if (result.Success)
            {
                var addresses = result.Answer as AddressPart[];

                CheckIfAnswerHasPII(state, PIIType, addresses[0].address1, "ADDRESS");
                CheckIfAnswerHasPII(state, PIIType, addresses[0].address2, "ADDRESS");
                CheckIfAnswerHasPII(state, PIIType, addresses[0].city, "CITY");
                CheckIfAnswerHasPII(state, PIIType, addresses[0].region, "STATE");
                CheckIfAnswerHasPII(state, PIIType, addresses[0].postalCode, "ZIPCODE");
            }
        }

    }
}