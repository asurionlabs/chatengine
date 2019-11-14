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
    public class DeviceParser : ChatParserBase
    {
        readonly DeviceCatalog deviceCatalog;

        public DeviceParser(DeviceCatalog deviceCatalog)
        {
            this.deviceCatalog = deviceCatalog;
        }

        public override Task<ParseResult> ParseAsync(ChatState state, Chat_ParseField chatParseField, ChatMessage message)
        {

            var tokens = TextClassificationService.Tokenize(message.UserInput);

            // HTC One and iPhone match words quite easily
            var cleanedTokens = (from t in tokens
                                 where t != "phone" && t != "phones" && t != "phone's" && t != "the" && t != "tone"
                                 select t.ToLower()).ToArray();

            (string matchedText, int matchedIndex, float ratio) = MatchService.FindMatch(cleanedTokens, 1, 4, deviceCatalog.MatchCharacters);
            var device = deviceCatalog.MakeModelList[matchedIndex];

            var deviceMatch = new DeviceMatchResult
            {
                Id = device.Id,
                Make = device.Make,
                Model = device.Model,
                DisplayName = device.DisplayName,
                IsUncommon = device.IsUncommon,
                Ratio = ratio
            };

            double minConfidence = ChatConfiguration.MinimumDeviceConfidence;
            if (device.IsUncommon)
                minConfidence = ChatConfiguration.MinimumUncommonDeviceConfidence;
            if (deviceMatch.Ratio > minConfidence)
                return Task.FromResult(ParseResult.CreateSuccess(deviceMatch));

            return Task.FromResult(ParseResult.Failed);
        }
    }
}