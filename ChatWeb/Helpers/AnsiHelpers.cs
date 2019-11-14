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
using System.Text;
using System.Web;

namespace ChatWeb.Helpers
{
    public sealed class AnsiHelpers
    {
        const int MinCharacterCode = 20;
        const int MaxCharacterCode = 127;

        static bool ContainsNonAnsi(string value)
        {
            // Check if contains non-ANSI
            for (int i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (c < MinCharacterCode || c > MaxCharacterCode)
                    return true;
            }
            return false;
        }

        public static string StripNonAnsi(string value)
        {
            if (String.IsNullOrEmpty(value) || !ContainsNonAnsi(value))
                return value;

            var ansiString = new StringBuilder();

            for (int i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (c >= MinCharacterCode && c <= MaxCharacterCode)
                    ansiString.Append(c);
            }

            return ansiString.ToString();
        }
    }
}