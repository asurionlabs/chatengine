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

namespace ChatWeb.Models
{
    /// <summary>
    /// Channel specific data.  This is used for logging, events, etc
    /// </summary>
    [Serializable]
    public class ChannelData
    {
        /// <summary>
        /// Channel Session Id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Prefix to be used for tag events and logging
        /// </summary>
        public string TagPrefix { get; set; }
    }
}