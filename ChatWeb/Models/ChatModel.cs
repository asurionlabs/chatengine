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
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChatWeb.Models
{
    [Serializable]
    public class ChatModel
    {
        static string AgentName = "Ava";

        public ChatModel() : this(null)
        {
        }

        public ChatModel(string agentName)
        {
            ChatId = Guid.NewGuid().ToString("N");
            CurrentState = new ChatState();

            if (String.IsNullOrEmpty(agentName))
                CurrentState.GlobalAnswers[ChatStandardField.AgentName] = AgentName;
            else
                CurrentState.GlobalAnswers[ChatStandardField.AgentName] = agentName;

            CurrentState.SessionData.AgentName = CurrentState.GlobalAnswers.GetFieldAnswer(ChatStandardField.AgentName);

            ChatTimeout = ChatConfiguration.DefaultChatTimeout;
        }

        public string ChatId { get; set; }
        // Client's session Id used for log tracking 
        public string ClientSessionId { get; set; }

        public ContextRule ContextRule { get; set; }
        public bool PassThru { get; set; }
        public int BadMessageCount { get; private set; }
        public int ConsecutiveBadMessageCount { get; private set; }

        public int ChatTimeout { get; set; }

        public bool AgentPaused { get; set; }

        public bool UseDebugFlows { get; set; }
        public string DebugUserId { get; set; }
        public bool UseNodeScripts { get; set; }

        public ChatState CurrentState { get; private set; }

        public string SessionLogId
        {
            get
            {
                if (!String.IsNullOrEmpty(ClientSessionId))
                    return ClientSessionId;

                return ChatId;
            }
        }
        public void IncreaseBadMessageCount()
        {
            BadMessageCount++;
            ConsecutiveBadMessageCount++;
        }

        public void ResetConsecutiveBadMessageCount()
        {
            ConsecutiveBadMessageCount = 0;
        }

    }
}
