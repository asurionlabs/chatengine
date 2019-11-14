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
    public class YesNoParser : ChatParserBase
    {
        static readonly ParseResult ParseResultYes = ParseResult.CreateSuccess("yes");
        static readonly ParseResult ParseResultNo = ParseResult.CreateSuccess("no");

        const string YesRegex = @"\byes\b|^ys$|^y$|^yss$|^yess$|^ya$|^yay$|^ye$|\byup\b|\byep\b|\byeah\b|:\+1:|:pray:|\byea\b|\byeas\b|\byeahs\b|\bsure\b|^yessir$|^yesir$|^i think so$|^ok$|^okay$|^looks good$|^sounds good$|^affirmative$|^right$|^fine$|^definitely$|^absolutely$|^totally$|^correct$";
        public const string NoRegex = @"\bno\b|^n$|\bnope\b|\bnopes\b|^nah$|^na$|^nay$|^ney$|^neh$|^not$|:middle_finger:|:-1:|^negative$|^not really$|^not now$|^bnot right now$|^not sure$";

        public override Task<ParseResult> ParseAsync(ChatState state, Chat_ParseField chatParseField, ChatMessage message)
        {
            string text = GetTextSource(chatParseField, message);

            if (Regex.IsMatch(text, YesRegex, RegexOptions.IgnoreCase))
                return Task.FromResult(ParseResultYes);
            else if (Regex.IsMatch(text, NoRegex, RegexOptions.IgnoreCase))
                return Task.FromResult(ParseResultNo);

            return Task.FromResult(ParseResult.Failed);
        }
    }
}