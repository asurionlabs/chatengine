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

namespace ChatWeb.Models
{
    public class TextParserResponse
    {
        public TextParserSentence[] Sentences { get; set; }
    }

    public class TextParserSentence
    {
        public int Index { get; set; }
        public TextParserEntitymention[] Entitymentions { get; set; }
        public TextParserToken[] Tokens { get; set; }
    }

    public class TextParserEntitymention
    {
        public int DocTokenBegin { get; set; }
        public int DocTokenEnd { get; set; }
        public int TokenBegin { get; set; }
        public int TokenEnd { get; set; }
        public string Text { get; set; }
        public int CharacterOffsetBegin { get; set; }
        public int CharacterOffsetEnd { get; set; }
        public string Ner { get; set; }
    }

    public class TextParserToken
    {
        public int Index { get; set; }
        public string Word { get; set; }
        public string OriginalText { get; set; }
        public string Lemma { get; set; }
        public int CharacterOffsetBegin { get; set; }
        public int CharacterOffsetEnd { get; set; }
        public string Pos { get; set; }
        public string Ner { get; set; }
        public string Before { get; set; }
        public string After { get; set; }
    }

}
