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

using JsonSubTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatEngine.Models
{
    [JsonConverter(typeof(JsonSubtypes), "type") ]
    [JsonSubtypes.KnownSubType(typeof(NodeAbortResponse), "abort")]
    [JsonSubtypes.KnownSubType(typeof(NodeActionResponse), "action")]
    [JsonSubtypes.KnownSubType(typeof(NodeAddPiiTextResponse), "addPiiText")]
    [JsonSubtypes.KnownSubType(typeof(NodeCallFlowResponse), "CallFlow")]
    [JsonSubtypes.KnownSubType(typeof(NodeChangeContextResponse), "changeContext")]
    [JsonSubtypes.KnownSubType(typeof(NodeConditionResponse), "condition")]
    [JsonSubtypes.KnownSubType(typeof(NodeErrorResponse), "error")]
    [JsonSubtypes.KnownSubType(typeof(NodeIncreaseFailureCountResponse), "increaseFailureCount")]
    [JsonSubtypes.KnownSubType(typeof(NodeLogResponse), "log")]
    [JsonSubtypes.KnownSubType(typeof(NodeMessageResponse), "message")]
    [JsonSubtypes.KnownSubType(typeof(NodePutKinesisEventResponse), "putKinesisEvent")]
    [JsonSubtypes.KnownSubType(typeof(NodeSendEmailResponse), "sendEmail")]
    [JsonSubtypes.KnownSubType(typeof(NodeSendTagEventResponse), "sendTagEvent")]
    [JsonSubtypes.KnownSubType(typeof(NodeSetQuickRepliesResponse), "setQuickReplies")]
    [JsonSubtypes.KnownSubType(typeof(NodeTransferToAgentResponse), "transferToAgent")]
    [JsonSubtypes.KnownSubType(typeof(NodeUiResponse), "ui")]
    [JsonSubtypes.KnownSubType(typeof(NodeVariableResponse), "variable")]

    public class INodeResponse
    {
        [JsonProperty("type")]
        virtual internal string ResponseType { get; }
    }

    public class NodeAbortResponse : INodeResponse
    {
        internal override string ResponseType { get; } = "abort";

        [JsonProperty("result")]
        public string Result { get; set; }
    }

    public class NodeActionResponse : INodeResponse
    {
        internal override string ResponseType { get; } = "action";

        [JsonProperty("result")]
        public object Result { get; set; }
    }

    public class NodeAddPiiTextResponse : INodeResponse
    {
        internal override string ResponseType { get; } = "addPiiText";

        [JsonProperty("result")]
        public NodeAddPiiTextResult Result { get; set; }
    }

    public class NodeAddPiiTextResult
    {
        [JsonProperty("piiType")]
        public int PiiType { get; set; }
        [JsonProperty("text")]
        public string Text { get; set; }
        [JsonProperty("mask")]
        public string Mask { get; set; }
    }

    public class NodeChangeContextResponse : INodeResponse
    {
        internal override string ResponseType { get; } = "changeContext";

        [JsonProperty("result")]
        public NodeChangeContextResult Result { get; set; }
    }

    public class NodeChangeContextResult
    {
        [JsonProperty("partner")]
        public string Partner { get; set; }
        [JsonProperty("context")]
        public string Context { get; set; }
    }

    public class NodeConditionResponse : INodeResponse
    {
        internal override string ResponseType { get; } = "condition";

        [JsonProperty("result")]
        public bool Result { get; set; }
    }

    public class NodeErrorResponse : INodeResponse
    {
        internal override string ResponseType { get; } = "error";

        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("stack")]
        public string Stack { get; set; }
    }

    public class NodeIncreaseFailureCountResponse : INodeResponse
    {
        internal override string ResponseType { get; } = "increaseFailureCount";

    }

    public class NodeCallFlowResponse : INodeResponse
    {
        internal override string ResponseType { get; } = "CallFlow";

        [JsonProperty("result")]
        public NodeCallFlowResult Result { get; set; }
    }

    public class NodeCallFlowResult
    {
        [JsonProperty("flowName")]
        public string FlowName { get; set; }
        [JsonProperty("params")]
        public object Params { get; set; }
    }

    public class NodeLogResponse : INodeResponse
    {
        internal override string ResponseType { get; } = "log";

        [JsonProperty("result")]
        public string Result { get; set; }
    }

    public class NodeMessageResponse : INodeResponse
    {
        internal override string ResponseType { get; } = "message";

        [JsonProperty("result")]
        public string Result { get; set; }
    }

    public class NodeUiResponse : INodeResponse
    {
        internal override string ResponseType { get; } = "ui";

        [JsonProperty("result")]
        public NodeUiResult Result { get; set; }
    }

    public class NodeUiResult
    {
        [JsonProperty("customMarkup")]
        public string CustomMarkup { get; set; }

        [JsonProperty("plainText")]
        public string PlainText { get; set; }
    }

    public class NodeSendEmailResponse : INodeResponse
    {
        internal override string ResponseType { get; } = "sendEmail";

        [JsonProperty("result")]
        public NodeSendEmailResult Result { get; set; }
    }

    public class NodeSendEmailResult
    {
        [JsonProperty("from")]
        public string From { get; set; }
        [JsonProperty("recipients")]
        public string Recipients { get; set; }
        [JsonProperty("subject")]
        public string Subject { get; set; }
        [JsonProperty("body")]
        public string Body { get; set; }
        [JsonProperty("isBodyHtml")]
        public bool IsBodyHtml { get; set; }
    }

    public class NodeSendTagEventResponse : INodeResponse
    {
        internal override string ResponseType { get; } = "ui";

        [JsonProperty("result")]
        public SendTagEventResult Result { get; set; }
    }

    public class SendTagEventResult
    {
        [JsonProperty("tagName")]
        public string TagName { get; set; }
        [JsonProperty("properties")]
        public object Properties { get; set; }
    }

    public class NodeSetQuickRepliesResponse : INodeResponse
    {
        internal override string ResponseType { get; } = "setQuickReplies";

        [JsonProperty("result")]
        public NodeSetQuickRepliesResult Result { get; set; }
    }

    public class NodeSetQuickRepliesResult
    {
        [JsonProperty("choices")]
        public object Choices { get; set; }
    }

    public class NodePutKinesisEventResponse : INodeResponse
    {
        internal override string ResponseType { get; } = "putKinesisEvent";

        [JsonProperty("result")]
        public PutKinesisEventResult Result { get; set; }
    }

    public class PutKinesisEventResult
    {
        [JsonProperty("arn")]
        public string Arn { get; set; }
        [JsonProperty("streamName")]
        public string StreamName { get; set; }
        [JsonProperty("stsRoleSessionName")]
        public string StsRoleSessionName { get; set; }
        [JsonProperty("partitionKey")]
        public string PartitionKey { get; set; }
        [JsonProperty("eventData")]
        public object EventData { get; set; }
    }

    public class NodeTransferToAgentResponse : INodeResponse
    {
        internal override string ResponseType { get; } = "transferToAgent";

        [JsonProperty("result")]
        public TransferToAgentResult Result { get; set; }
    }

    public class TransferToAgentResult
    {
        [JsonProperty("transferInfo")]
        public object TransferInfo { get; set; }
        [JsonProperty("skill")]
        public string Skill { get; set; }
    }

    public class NodeVariableResponse : INodeResponse
    {
        internal override string ResponseType { get; } = "variable";

        [JsonProperty("result")]
        public Dictionary<string, object> Result { get; set; }
    }
}
