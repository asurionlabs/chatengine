﻿/*
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
    public class LogProperty
    {
        public string partner { get; set; }
        public string context { get; set; }
        public string channel { get; set; }
        public string distinct_id { get; set; }
        public string utm_campaign { get; set; }
        public string utm_medium { get; set; }
        public string utm_source { get; set; }
    }
}
