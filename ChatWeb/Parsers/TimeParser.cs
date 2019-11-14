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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace ChatWeb.Parsers
{
    public class TimeParser : ChatParserBase
    {
        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        readonly LuisService luisService;

        public TimeParser(LuisService luisService)
        {
            this.luisService = luisService;
        }

        public override async Task<ParseResult> ParseAsync(ChatState state, Chat_ParseField chatParseField, ChatMessage message)
        {
            if (message.LuisDateOutput == null)
                message.LuisDateOutput = await luisService.Parse(message.UserInput, state.GetUserTimeZoneOffset());

            if (message.LuisDateOutput?.Entities.Length > 0)
            {
                var v1Entities = ExtractV1TimeEntities(state, message.LuisDateOutput);
                if (v1Entities.Length > 0)
                    return ParseResult.CreateSuccess(v1Entities);
           }

            logger.DebugFormat("Parse: Luis Time Parser got nothing. - {0}", (message.LuisDateOutput != null) ? JsonConvert.SerializeObject(message.LuisDateOutput) : "null");

            return ParseResult.Failed;
        }

        private static string[] ExtractV1TimeEntities(ChatState state, LuisResponse luisResponse)
        {
            LuisDateParser parser = new LuisDateParser(state.GetUserTimeZoneOffset());

            var luisTimes = parser.ParseLuisTimes(luisResponse.Entities);

            return (from time in luisTimes
                    where time.Time != null
                    select time.Time.Format12Hour).ToArray();
        }
    }
}