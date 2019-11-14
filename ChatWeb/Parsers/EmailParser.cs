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
using System.Text.RegularExpressions;

namespace ChatWeb.Parsers
{
    public class EmailParser : ChatParserBase
    {
        public const string EmailRegex = @"\b(([a-z0-9!#$%&'*+\/=?^_`{|}~.-]+)@([a-z0-9]([a-z0-9-]*[a-z0-9])?(\.[a-z0-9]([a-z0-9-]*[a-z0-9])?)+))\b";
        public const string PartialEmailRegex = @"\b(([a-z0-9!#$%&'*+\/=?^_`{|}~.-]+)@([a-z0-9]([a-z0-9-]*[a-z0-9])?))\b";

        readonly bool searchMode;
        string foundPartial;

        public EmailParser(bool searchMode)
        {
            this.searchMode = searchMode;
            this.PIIMask = "EMAIL";
            this.PIIType = PIIType.Low;
        }

        public override Task<ParseResult> ParseAsync(ChatState state, Chat_ParseField chatParseField, ChatMessage message)
        {
            string text = GetTextSource(chatParseField, message);

            var result = MatchEmail(EmailRegex, text);
            if (result.Success)
                return Task.FromResult(result);

            if (!searchMode)
            {
                var partial = MatchEmail(PartialEmailRegex, text);
                if (partial.Success)
                    foundPartial = partial.Answer as string;
            }

            return Task.FromResult(result);
        }

        public override void UpdateState(ChatState state, ChatFlowStep flowStep, Chat_ParseField chatParseField, ParseResult result)
        {
            var variables = GetChatVariables(state, flowStep, chatParseField.VarScope);

            if (result.Success)
            {
                variables[chatParseField.FieldName] = result.Answer;
                CheckIfAnswerHasPII(state, PIIType, result.Answer.ToString(), PIIMask);
            }

            if (!String.IsNullOrEmpty(foundPartial))
                variables[$"{chatParseField.FieldName}_Try"] = foundPartial + ".com";
        }

        ParseResult MatchEmail(string regex, string text)
        {
            var match = Regex.Match(text, regex, RegexOptions.IgnoreCase);
            if (match.Success)
                return ParseResult.CreateSuccess(match.Captures[0].Value);

            return ParseResult.Failed;
        }
    }
}