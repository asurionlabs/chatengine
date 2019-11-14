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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChatWeb.Helpers
{
    public class EmojiParser
    {
        readonly Dictionary<string, string> emojiMap;
        readonly Regex emojiMatcher;

        public EmojiParser(string configPath)
        {
            // load emoji dictionary
            var data = File.ReadAllLines(Path.Combine(configPath, @"emoji.csv"));
            emojiMap = new Dictionary<string, string>();
            foreach (var line in data)
            {
                var fields = line.Split(',');
                emojiMap.Add(fields[0], ":" + fields[1] + ":");
            }

            emojiMatcher = new Regex(String.Join("|", emojiMap.Keys.Select(Regex.Escape)));
        }

        public string ReplaceEmoji(string input)
        {
            if (input == null)
                return null;

            return emojiMatcher.Replace(input, match => emojiMap[match.Value]);
        }
    }
}
