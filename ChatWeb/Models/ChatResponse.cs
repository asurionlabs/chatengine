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
using System.Threading.Tasks;

namespace ChatWeb.Models
{
    /// <summary>
    /// Response object from server
    /// </summary>
    public class ChatResponse
    {
        public ChatResponse()
        {
        }

        /// <summary>
        /// ChatId of the conversation.  Use this in future requests in the same conversation
        /// </summary>
        public string ChatId { get; set; }

        /// <summary>
        /// Text messages for response to the provided input text.
        /// </summary>
        public string[] Messages { get; set; }

        /// <summary>
        /// Text messages for response to the provided input text.
        /// </summary>
        public UiMessage[] UiMessages { get; set; }

        /// <summary>
        /// Name of the Agent.  This can be configured on the server to provide a random agent name.
        /// Currently always set to Ava.
        /// </summary>
        public string AgentName { get; set; }
        /// <summary>
        /// Current Flow Stack
        /// </summary>
        public string[] Flows { get; set; }
        /// <summary>
        /// Flow Steps taken for this response
        /// </summary>
        public ChatStepId[] Steps { get; set; }
        /// <summary>
        /// Entity data pulled from user's conversation
        /// </summary>
        public Dictionary<string, object> UserData { get; set; }
        /// <summary>
        /// Obsolete.  Clients should use the ChatStatus.Error field
        /// </summary>
        public string Error { get; set; }
        /// <summary>
        /// Version of the API.  This is typically a date/time stamp
        /// </summary>
        public string Version { get; set;  }

        public string PlaceholderText { get; set; }

        /// <summary>
        /// Possible expected answers a user can select from
        /// </summary>
        public string[] PossibleUserAnswers { get; set; }

        /// <summary>
        /// Possible expected answers a user can select.  Offers more advanced options than PossibleUserAnswers property
        /// </summary>
        public UserChoice[] UserChoices { get; set; }

        /// <summary>
        /// Tip to client about the type of input expected as a user response
        /// </summary>
        public string UserInputType { get; set; }

        /// <summary>
        /// Obsolete.  Clients should use the ChatStatus.ShouldEndChat field
        /// </summary>
        public bool ShouldEndChat { get; set; }

        /// <summary>
        /// Obsolete.  Clients should use the ChatStatus.TransferToAgent field
        /// </summary>
        public bool TransferToAgent { get; set; }

        /// <summary>
        /// Obsolete.  Clients should use the ChatStatus.TransferInfo field
        /// </summary>
        public object TransferInfo { get; set; }

        public DebugDataResponse DebugData { get; set; }

        /// <summary>
        /// Provides status of the chat
        /// </summary>
        public ChatStatus Status { get; set; }

        /// <summary>
        /// Trusted Client data.  This data is only available for Trusted Clients.
        /// </summary>
        public IDictionary<string, object> TrustedClientData { get; set; }

    }

    public class ChatStatus
    {
        /// <summary>
        /// Indicates the current flow path ended (resolved) and a new line of questioning started
        /// </summary>
        public bool FlowPathRestarted { get; set; }

        /// <summary>
        /// Flag to indicate the chat session has expired
        /// </summary>
        public bool SessionExpired { get; set; }

        /// <summary>
        /// Any errors message if exceptions occur on the server.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Flag to indicate the engine did not understand the user's input.
        /// </summary>
        public bool FailToUnderstand { get; set; }

        /// <summary>
        /// Flag to indicate the end of a chat.  In some cases, the user can still start a new conversation with the existing session.
        /// </summary>
        public bool ShouldEndChat { get; set; }

        /// <summary>
        /// Flag to indiacte the chat should be transferred to a live agent if possible
        /// </summary>
        public bool TransferToAgent { get; set; }

        /// <summary>
        /// Information to pass to live agent while transferring chat.
        /// </summary>
        public object TransferInfo { get; set; }

        /// <summary>
        /// Agent skill to use for the transfer
        /// </summary>
        public string TransferToAgentSkill { get; set; }

        /// <summary>
        /// Wait time for next agent with specified skill
        /// </summary>
        public int WaitTimeForNextAgent { get; set; }
    }
    public class DebugDataResponse
    {
        /// <summary>
        /// Current Flow Stack
        /// </summary>
        public string[] Flows { get; set; }
        /// <summary>
        /// Flow Steps taken for this response
        /// </summary>
        public ChatStepId[] Steps { get; set; }
        /// <summary>
        /// Current chat sessions state variables
        /// </summary>
        public Dictionary<string, object> Variables { get; set; }
        
        /// <summary>
        /// Current chat sessions state Local flow variables, for the last step in the current flow response
        /// </summary>
        public Dictionary<string, Dictionary<string, object>> LocalVariables { get; set; }

    }
}
