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
using System.Web;

namespace ChatWeb.Models
{
    public class DeviceModel
    {
        public DeviceModel(string id, string make, string model, string matchtText)
        {
            Id = id;
            Make = make;
            Model = model;
            MakeModelChars = matchtText.ToCharArray();
            MakeModelLength = matchtText.Length;
        }

        public string Id { get; private set; }
        public string Make { get; private set; }
        public string Model { get; private set; }
        public char[] MakeModelChars { get; private set; }
        public int MakeModelLength { get; private set; }

        public string DisplayName { get; set; }
        public bool IsUncommon { get; set; }
    }
}