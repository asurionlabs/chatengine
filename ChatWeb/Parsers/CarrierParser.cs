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
using ChatWeb.RegexProviders;

namespace ChatWeb.Parsers
{
    public class CarrierParser : ChatParserBase
    {
        static readonly RegexCarrier Carriers = new RegexCarrier();

        public override Task<ParseResult> ParseAsync(ChatState state, Chat_ParseField chatParseField, ChatMessage message)
        {
            // Check both corrected and original input, since our spell checker can't handle at&t properly.
            var result = MatchCarrier(message.CorrectedUserInput);

            if (result.Success)
                return Task.FromResult(result);

            return Task.FromResult(MatchCarrier(message.UserInput));
        }

        ParseResult MatchCarrier(string text)
        {
            foreach (var carrier in Carriers.ProviderNames)
            {
                if (Carriers.IsMatch(text, carrier))
                    return ParseResult.CreateSuccess(carrier);
            }

            return ParseResult.Failed;
        }
    }
}