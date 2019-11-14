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
    public class LossCategoryParser : ChatParserBase
    {
        static ParseResult ParseResultNoneList = ParseResult.CreateSuccess(new string[] { "None" });

        readonly LuisService luisService;

        public LossCategoryParser(LuisService luisService)
        {
            this.luisService = luisService;
        }

        public override async Task<ParseResult> ParseAsync(ChatState state, Chat_ParseField chatParseField, ChatMessage message)
        {
            if (message.LuisDamageClassifierOutput == null)
                message.LuisDamageClassifierOutput = await luisService.Parse(message.CorrectedUserInput, 0);

            string[] intents = message.LuisDamageClassifierOutput.GetIntents(ChatConfiguration.MinLuisConfidenceRatio);

            if (intents?.Length > 0)
                return ParseResult.CreateSuccess(intents);

            // AVA-997: Always use None if we didn't parse anything.
            return ParseResultNoneList;
        }

        public override void UpdateState(ChatState state, ChatFlowStep flowStep, Chat_ParseField chatParseField, ParseResult result)
        {
            if (result.Success)
            {
                var variables = GetChatVariables(state, flowStep, chatParseField.VarScope);

                var intents = result.Answer as string[];
                variables[chatParseField.FieldName] = intents[0];
                variables[chatParseField.FieldName + "List"] = intents;
            }
        }
    }
}