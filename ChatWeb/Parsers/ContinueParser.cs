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
    public class ContinueParser : ChatParserBase
    {
        public const string ContinueRegex = @"^yes$|^ys$|^y$|^ok$|^okay$|^""ok""$|^continue$";
        static readonly ParseResult ParseResultYes = ParseResult.CreateSuccess("yes");
        static readonly ParseResult ParseResultNo = ParseResult.CreateSuccess("no");

        public override Task<ParseResult> ParseAsync(ChatState state, Chat_ParseField chatParseField, ChatMessage message)
        {
            if (String.Compare(message.CorrectedUserInput, "view solution", true) == 0)
                return Task.FromResult(ParseResult.CreateSuccess("viewedSolution"));
            else if (Regex.IsMatch(message.CorrectedUserInput, ContinueRegex, RegexOptions.IgnoreCase))
                return Task.FromResult(ParseResultYes);
            else if (Regex.IsMatch(message.CorrectedUserInput, YesNoParser.NoRegex, RegexOptions.IgnoreCase))
                return Task.FromResult(ParseResultNo);

            return Task.FromResult(ParseResult.Failed);
        }
    }
}