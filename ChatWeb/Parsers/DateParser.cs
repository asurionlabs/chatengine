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
using Newtonsoft.Json;
using ChatWeb.Services;

namespace ChatWeb.Parsers
{
    public class DateParser : ChatParserBase
    {
        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        readonly int version;
        readonly LuisService luisService;

        public DateParser(int version, LuisService luisService)
        {
            this.version = version;
            this.luisService = luisService;
        }

        public override async Task<ParseResult> ParseAsync(ChatState chatState, Chat_ParseField chatParseField, ChatMessage message)
        {
            int timezoneOffset = chatState.GetUserTimeZoneOffset();

            if (message.LuisDateOutput == null)
                message.LuisDateOutput = await luisService.Parse(message.UserInput, timezoneOffset);

            var assumeFuture = false;
            if (bool.TryParse(chatParseField?.GetProperty("AssumeFuture"), out bool test))
                assumeFuture = test;
            
            var results = ExtractDateResults(chatState, message.LuisDateOutput, assumeFuture);

            if ((results == null) || (results.Length == 0))
            {
                logger.InfoFormat("Parse: luis date parser got nothing. - {0}", (message.LuisDateOutput != null) ? JsonConvert.SerializeObject(message.LuisDateOutput) : "null");
                return ParseResult.Failed;
            }

            if (version == 1)
                return ProcessAsV1Date(message, timezoneOffset, results);

            return ParseResult.CreateSuccess(results);
        }

        private DateTimeParseResult[] ExtractDateResults(ChatState chatState, LuisResponse luisResponse, bool assumeFuture)
        {
            LuisDateParser parser = new LuisDateParser(chatState.GetUserTimeZoneOffset());

            if (luisResponse?.Entities.Length > 0)
                return parser.ParseLuisDates(luisResponse.Entities, assumeFuture);

            return null;
        }

        public override void UpdateState(ChatState state, ChatFlowStep flowStep, Chat_ParseField chatParseField, ParseResult parseResult)
        {
            if (!parseResult.Success)
                return;

            var variables = GetChatVariables(state, flowStep, chatParseField.VarScope);

            if (version == 1)
            {
                UpdateV1State(state, chatParseField, parseResult, variables);
                return;
            }

            CheckIfDatesHavePII(state, chatParseField, parseResult);

            variables[chatParseField.FieldName] = parseResult.Answer;
        }

        private void CheckIfDatesHavePII(ChatState state, Chat_ParseField chatParseField, ParseResult parseResult)
        {
            DateTimeParseResult[] results = (DateTimeParseResult[])parseResult.Answer;
            foreach (var result in results)
            {
                if (result.DateTime.HasValue)
                    CheckIfAnswerHasPII(state, chatParseField, result.DateTime.Value.ToShortDateString(), PIIMask);
            }
        }

        #region V1 Date support
        private ParseResult ProcessAsV1Date(ChatMessage message, int timezoneOffset, DateTimeParseResult[] results)
        {
            var v1LuisDate = ExtractV1DateResults(results, timezoneOffset);
            if (v1LuisDate == null)
            {
                logger.InfoFormat("Parse: luis date parser got nothing. - {0}", (message.LuisDateOutput != null) ? JsonConvert.SerializeObject(message.LuisDateOutput) : "null");
                return ParseResult.Failed;
            }

            return ParseResult.CreateSuccess(v1LuisDate.Value);
        }

        DateTime? ExtractV1DateResults(DateTimeParseResult[] results, int timezoneOffset)
        {
            if (results?.Length > 0)
            {
                var date = (from time in results
                            where time.DateTime != null
                            select time.DateTime).FirstOrDefault();

                if (date != null)
                    return AttachLocalizedTime(date.Value, timezoneOffset);
            }

            return null;
        }

        private DateTime AttachLocalizedTime(DateTime date, int timezoneOffset)
        {
            // Luis gets the user's timezone and localizes the date.  But we need to attach the users current hour to the date to assist with conversions
            // to other time zones.  Time zones can affect the actual date.
            // 1. Create date object including user's current time.
            // 2. Adjust to UTC to form proper DateTime UTC object.
            var now = DateTime.UtcNow;

            var usersCurrentHour = CalculateUsersCurrentHour(now.Hour, timezoneOffset);
            var adjustedDate = new DateTime(date.Year, date.Month, date.Day, usersCurrentHour, now.Minute, now.Second, DateTimeKind.Utc);

            // Convert back to true UTC
            return adjustedDate.AddHours(-timezoneOffset);
        }

        //WORKS for SErver in UTC.  Need to fix for server in PST
        static int CalculateUsersCurrentHour(int utcHour, int offset)
        {
            var target = utcHour + offset;
            if (target >= 24)
                target = target - 24;
            if (target < 0)
                target = target + 24;

            return target;
        }

        private void UpdateV1State(ChatState state, Chat_ParseField chatParseField, ParseResult result, ChatVariables variables)
        {
            DateTime date = (DateTime)result.Answer;
            int offset = state.GetUserTimeZoneOffset();

            variables[chatParseField.FieldName] = date.AddHours(offset).ToShortDateString();
            variables[chatParseField.FieldName + "_Date"] = date;

            CheckIfAnswerHasPII(state, chatParseField, date.ToString(), PIIMask);
        }
        #endregion

    }
}