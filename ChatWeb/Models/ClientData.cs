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
    [Serializable]
    public class ClientData
    {
        public string AlternateMdn { get; set; }
        public string ArticleId { get; set; }
        public string Fingerprint { get; set; }
        public string ClientName { get; set; }
        public string ClientVersion { get; set; }
        public string ClientIp { get; set; }
        public bool IsDesktopUser { get; set; }
        public string SubscriberMdn { get; set; }
        public string UserAgent { get; set; }
        public int TimeZoneOffset { get; set; }
        public string UiResult { get; set; }
    }
}