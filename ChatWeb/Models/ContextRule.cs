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
using System.Collections.Generic;

namespace ChatWeb.Models
{
    [DynamoDBTable("Chat_ContextRule")]
    [Serializable]
    public class ContextRule
    {
        [DynamoDBHashKey]
        public string Context { get; set; }
        /// <summary>
        /// Specifies the default classifier to call.  If null, the main classifier is called.
        /// </summary>
        public string DefaultClassifier { get; set; }
        /// <summary>
        /// Specifies if the engine should try to auto-classify any messages that were not understood.
        /// Setting to true, will tell the engine to skip auto-classify.  Note that "common chat" is still processed.
        /// </summary>
        public bool IgnoreAutoClassify { get; set; }
        /// <summary>
        /// Specifies if the engine should call the UnsupportedFlow to inform the user we understood the message, but the current context does not allow it.
        /// Setting to true, will tell the engine to handle the message as not understood.
        /// </summary>
        public bool IgnoreUnsupportedFlowMessage { get; set; }

        /// <summary>
        /// List of flows that are allowed to be called from this context.  Each flow name can use regular expression matching.
        /// </summary>
        public List<String> AllowedIntentFlows { get; set; }
    }
}