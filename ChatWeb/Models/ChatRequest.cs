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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatWeb.Models
{
    /// <summary>
    /// Represents the text input from the user
    /// </summary>
    public class ChatRequest
    {
        /// <summary>
        /// Raw text from the User's chat message
        /// </summary>
        public string UserInput { get; set; }
        /// <summary>
        /// ChatId for this conversation
        /// </summary>
        public string ChatId { get; set; }

        /// <summary>
        /// Optional Partner the chat will be for. 
        /// </summary>
        public string Partner { get; set; }

        /// <summary>
        /// Optional Context the chat will be for.  
        /// </summary>
        public string Context { get; set; }

        /// <summary>
        /// Optional TimeZone offest from UTC.  Example:  -0800  PST
        /// </summary>
        public string TimeZone { get; set; }

        /// <summary>
        /// Optional Name of the Agent to put in the messages.  If blank, AVA will be used.
        /// </summary>
        public string AgentFirstName { get; set; }

        /// <summary>
        /// Channel the client is connecting from.  Examples:  liveperson, web, virtualagent, facebook
        /// </summary>
        public string Channel { get; set; }

        /// <summary>
        /// Type of input.  Examples:  KEYBOARD, VOICE
        /// </summary>
        public string InputType { get; set; }

        /// <summary>
        /// Channel specific data
        /// </summary>
        public ChannelData ChannelData { get; set; }

        /// <summary>
        /// Client specific data
        /// </summary>
        public ChatClientData ClientData { get; set; }

        /// <summary>
        /// Trusted Client data.  This data is ignored if the Trusted Client Key is not included in the request header.
        /// </summary>
        public IDictionary<string, object> TrustedClientData { get; set; }

        /// <summary>
        /// Data used for debugging flows.  Only works when server has debugging mode enabled.
        /// </summary>
        public ChatDebugData DebugData { get; set; }

        /// <summary>
        /// (Optional) Name of the user if known.  This will be used instead of asking the user.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Length of time to wait for the chat to timeout in minutes.  Maximum of 60 minutes.
        /// </summary>
        public int ChatTimeout { get; set; }

        /// <summary>
        /// Indicates if the agent has paused the chat
        /// </summary>
        public bool AgentPaused { get; set; }
    }

    /// <summary>
    /// Collection of client data to be used in a flow
    /// </summary>
    public class ChatClientData
    {
        /// <summary>
        /// Estimated wait time for a default skill live agent in case of transfer
        /// </summary>
        public int EstimatedLiveAgentWaitTime { get; set; }

        /// <summary>
        /// Used internally to process unknown properties
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, JToken> Properties;

        /// <summary>
        /// Allow the UI to send an Article ID to the flow so the flow can display the selected article as a chat
        /// </summary>
        public string ArticleId { get; set; }
        /// <summary>
        /// Claims information
        /// </summary>
        public string ClaimsPod { get; set; }

        /// <summary>
        /// Utm Source information
        /// </summary>
        public string UtmSource { get; set; }
        /// <summary>
        /// Utm Medium information
        /// </summary>
        public string UtmMedium { get; set; }
        /// <summary>
        /// Utm Campaign information
        /// </summary>
        public string UtmCampaign { get; set; }

        /// <summary>
        /// Clients Distinct Id
        /// </summary>
        public string Distinct_Id { get; set; }
        /// <summary>
        /// Client fingerprint
        /// </summary>
        public string Fingerprint { get; set; }
        /// <summary>
        /// Client name.  Should be browser name if applicable.  ex: Chrome
        /// </summary>
        public string ClientName { get; set; }
        /// <summary>
        /// Client version.  Version of the browser if applicable
        /// </summary>
        public string ClientVersion { get; set; }
        /// <summary>
        /// IP address of the client.  This is ignored and automatically determined through other methods
        /// </summary>
        public string ClientIp { get; set; }
        /// <summary>
        /// User agent of the client browser if applicable
        /// </summary>
        public string UserAgent { get; set; }
        /// <summary>
        /// Custom result for specific scenarios sent from the UI.
        /// </summary>
        public string UiResult { get; set; }

        public bool IsDesktopUser { get; set; }

        public string SubscriberMdn { get; set; }

        public string AlternateMdn { get; set; }

    }

    /// <summary>
    /// Collection of debug data to be used by the flow engine
    /// </summary>
    public class ChatDebugData
    {
        /// <summary>
        /// Set to true to set X-DummyMode header to other API's for testing
        /// </summary>
        public bool DummyMode { get; set; }

        /// <summary>
        /// Tells engine to start with the specified flow name
        /// </summary>
        public string StartFlowName { get; set; }
        
        /// <summary>
        /// Tells engine to start with the specified flow step id
        /// </summary>
        public string StartFlowId { get; set; }

        /// <summary>
        /// Tells the engine to try to load flow steps from a Debug table
        /// </summary>
        public bool UseDebugFlow { get; set; }

        /// <summary>
        /// Dictionary of variables to set in the engine
        /// </summary>
        public Dictionary<string, object> SetVariables { get; set; }

        /// <summary>
        /// User Id for attaching NodeJS debugger
        /// </summary>
        public string UserId { get; set; }
    }

}
