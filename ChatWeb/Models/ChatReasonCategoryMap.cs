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

using Amazon.DynamoDBv2.DataModel;
using System;

namespace ChatWeb.Models
{
    [DynamoDBTable("Chat_Reason_Category_Map")]
    public class ChatReasonCategoryMap
    {
        [DynamoDBHashKey("subcategory")]
        public string subcategory { get; set; }
        public string category { get; set; }
        public string reason { get; set; }
        public string reasonsub { get; set; }
        public string screason { get; set; }
        public string scsubreason { get; set; }
        public string sub { get; set; }
        public string subreason { get; set; }
    }
}