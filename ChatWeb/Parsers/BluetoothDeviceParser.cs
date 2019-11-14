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
    public class BluetoothDeviceParser : ChatParserBase
    {
        const string BluetoothDeviceNoRegex = @"\bheadset\b|\bjabra\b|\bcar\b|\bspeaker\b";

        public override Task<ParseResult> ParseAsync(ChatState state, Chat_ParseField chatParseField, ChatMessage message)
        {
            var matches = Regex.Matches(message.CorrectedUserInput, BluetoothDeviceNoRegex, RegexOptions.IgnoreCase);
            if (matches.Count > 0)
            {
                string[] apps = new string[matches.Count];

                for (int i = 0; i < matches.Count; i++)
                {
                    apps[i] = matches[i].Value;
                }

                return Task.FromResult(ParseResult.CreateSuccess(apps));
            }

            return Task.FromResult(ParseResult.Failed);
        }

    }
}