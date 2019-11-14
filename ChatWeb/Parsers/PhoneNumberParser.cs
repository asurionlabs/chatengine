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
using System.Text;
using System.Text.RegularExpressions;

namespace ChatWeb.Parsers
{
    public class PhoneNumberParser : ChatParserBase
    {
        public const string PhoneNumberRegex = @"\b1?\W*((\d{3})\W*(\d{3})\W*(\d{4}))\b";

        readonly bool searchMode;
        bool foundBadNumber;

        public PhoneNumberParser(bool searchMode)
        {
            this.searchMode = searchMode;
            this.PIIMask = "PHONE";
            this.PIIType = PIIType.Low;
        }

        public override Task<ParseResult> ParseAsync(ChatState state, Chat_ParseField chatParseField, ChatMessage message)
        {
            string text = GetTextSource(chatParseField, message);

            var result = MatchPhoneNumber(text);

            if (result.Success)
                return Task.FromResult(result);

            if (!searchMode)
            {
                result = MatchBadPhoneNumber(text);
                if (result.Success)
                    foundBadNumber = true;
            }

            return Task.FromResult(result);
        }

        public override void UpdateState(ChatState state, ChatFlowStep flowStep, Chat_ParseField chatParseField, ParseResult result)
        {
            if (result.Success)
            {
                var variables = GetChatVariables(state, flowStep, chatParseField.VarScope);

                if (foundBadNumber)
                {
                    variables[$"{chatParseField.FieldName}_Bad"] = true;
                }
                else
                {
                    var rawNumber = StripToNumber(result.Answer as string);

                    variables[chatParseField.FieldName] = rawNumber;
                    variables[$"{chatParseField.FieldName}_Bad"] = null;

                    // Set both exact user input, and parsed raw number as pii
                    CheckIfAnswerHasPII(state, PIIType, rawNumber, PIIMask);
                    CheckIfAnswerHasPII(state, PIIType, result.Answer as string, PIIMask);
                }
            }

        }

        static ParseResult MatchPhoneNumber(string text)
        {
            var match = Regex.Match(text, PhoneNumberRegex);
            if (match.Success)
                return ParseResult.CreateSuccess(match.Captures[0].Value);

            return ParseResult.Failed;
        }

        static ParseResult MatchBadPhoneNumber(string text)
        {
            string strip = StripToNumber(text);
            if ((strip.Length >= ChatConfiguration.MinimumBadPhoneDigitCount) && (strip.Length <= ChatConfiguration.MaximumBadPhoneDigitCount))
                return ParseResult.CreateSuccess(strip);

            return ParseResult.Failed;
        }

        static string StripToNumber(string str)
        {
            StringBuilder sb = new StringBuilder(16);
            foreach (char c in str)
            {
                if (c >= '0' && c <= '9')
                    sb.Append(c);
            }
            return sb.ToString();
        }

    }
}