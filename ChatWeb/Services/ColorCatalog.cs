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
using System.Web;

namespace ChatWeb.Services
{
    public class ColorCatalog
    {
        public (string color, string name, char[] colorChars)[] Colors;
        public char[][] ColorCodeChars;

        public void LoadCatalog(string path)
        {
            var list = new List<(string, string, char[])>();

            foreach (var line in File.ReadLines(path))
            {
                if (line.StartsWith("Color"))
                    continue;

                var fields = line.Split(',');
                if (fields.Length != 2)
                    continue;

                list.Add(GetColorItem(fields[0].ToLower(), fields[1]));
            }

            Colors = list.ToArray();
            ColorCodeChars = Colors.Select(c => { return c.colorChars; }).ToArray();
        }

        static (string color, string name, char[] colorChars) GetColorItem(string colorCode, string name)
        {
            return (colorCode, name, colorCode.ToCharArray());
        }
    }
}