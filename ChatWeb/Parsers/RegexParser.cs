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
    public class RegexParser : ChatParserBase
    {
        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        

        public override Task<ParseResult> ParseAsync(ChatState state, Chat_ParseField chatParseField, ChatMessage message)
        {
            string text = GetTextSource(chatParseField, message);

            // Shortcut for common case where flow tries to take all user input.
            // .* regex means all characters EXCEPT newlines. But the flow wants all including newlines
            // so running regex to collect all data doesn't make sense. So we just set the variable to everything.
            if (chatParseField.RuleData == ".*")
                return Task.FromResult(ParseResult.CreateSuccess(text));

            string regex = chatParseField?.GetProperty("Regex") ?? chatParseField.RuleData;

            try
            {
                var match = Regex.Match(text, regex, RegexOptions.IgnoreCase, ChatConfiguration.RegexTimeout);
                if (match.Success)
                {
                    if (String.IsNullOrEmpty(chatParseField.Answer))
                        return Task.FromResult(ParseResult.CreateSuccess(match.Value));

                    return Task.FromResult(ParseResult.CreateSuccess(chatParseField.Answer));
                }
            }
            catch (RegexMatchTimeoutException)
            {
                logger.ErrorFormat("Regex timed out. {0}", regex);
            }

            return Task.FromResult(ParseResult.Failed);
        }

    }
}