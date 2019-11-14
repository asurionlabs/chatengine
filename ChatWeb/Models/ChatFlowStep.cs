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
using ChatWeb.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatWeb.Models
{
    [DynamoDBTable("Chat_FlowStep")]
    [Serializable]
    public class ChatFlowStep
    {
        [DynamoDBHashKey]
        public string Flow { get; set; }
        [DynamoDBRangeKey]
        public string Id { get; set; }
        public string Name { get; set; }
        public string ObjectType { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        [DynamoDBProperty(typeof(UnknownEnumConverter<ChatSubType>))]
        public ChatSubType SubType { get; set; }
        public string Message { get; set; }
        public string ProbingQuestion { get; set; }
        public List<Chat_CarrierMessage> CarrierMessages { get; set; }
        public List<Chat_ChildRef> Children { get; set; }
        public List<Chat_ParseField> ParseFields { get; set; }
        public List<string> Actions { get; set; }
        public List<string> QuickReplies { get; set; }
        public List<UserChoice> UserChoices { get; set; }
        public string KBUrl { get; set; }
        public string ErrorHandler { get; set; }
        public string PlaceholderText { get; set; }
        public string UserInputType { get; set; }

        [DynamoDBProperty("location")]
        public string Location { get; set; }

        public string GetText()
        {
            if (!String.IsNullOrEmpty(Message))
                return Message;

            return ProbingQuestion;
        }


        //BUG: AVA-2254: Allow this to be a property on the object in the database set by the flow tool
        [DynamoDBIgnore]
        public bool AllowRetryClassificationWithBotText
        {
            get
            {
                if (ParseFields == null)
                    return false;

                return ParseFields.Any(field => field.ParseType == ChatRuleType.Parse && field.RuleData != "None");
            }
        }
    }

    [Serializable]
    public class UserChoice
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string ImageUrl { get; set; }
        public List<string> Options { get; set; }
        public string Type { get; set; }
    }

    public enum VariableScope
    {
        None = 0,
        Global = 1,
        Local = 2
    }

    public enum PIIType
    {
        None = 0,
        Low = 1,
        High = 2
    }

    [Serializable]
    public class Chat_ParseField
    {
        public string FieldName { get; set; }
        public string ParseType { get; set; }
        public string SourceData { get; set; }
        /// <summary>
        /// Used by regex to specify the regex to use.
        /// Used by parse to specify a default classification if none found.
        /// </summary>
        public string RuleData { get; set; }
        /// <summary>
        /// For some Parse Types, if the parse is successful, it will set the field to the Answer value
        /// </summary>
        public string Answer { get; set; }

        /// <summary>
        /// Flag to tell the engine to search previous messages for the answer before asking the user.
        /// </summary>
        public bool CheckPreviousMessages { get; set; }

        /// <summary>
        /// Flag to tell the engine not to generate automatic quick replies for this parse field
        /// </summary>
        public bool IgnoreQuickReplies { get; set; }

        /// <summary>
        /// Flag to indicate if the resulting variable is a global or local variable.
        /// Eventually, all will be local, but its an option for backwards compatibility
        /// with existing flows.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [DynamoDBProperty(typeof(UnknownEnumConverter<VariableScope>))]
        public VariableScope VarScope { get; set; }

        /// <summary>
        /// Flag to tell the answer the matched answer to this should be considered PII data.  (personally identifiable information).
        /// Valid values are "Low", "High"
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [DynamoDBProperty(typeof(UnknownEnumConverter<PIIType>))]
        public PIIType PIIType { get; set; }

        public string GetProperty(string name)
        {
            var ruleData = RuleDataObject;

            if (ruleData == null)
                return null;

            if ((ruleData.Properties == null) || !ruleData.Properties.ContainsKey(name))
                return null;

            return ruleData.Properties[name].ToString();
        }

        [DynamoDBIgnore]
        public RuleData RuleDataObject
        {
            get
            {
                if (RuleData == null)
                    return null;

                if (RuleData.StartsWith("{"))
                {
                    try
                    {
                        return JsonConvert.DeserializeObject<RuleData>(RuleData);
                    }
                    catch (JsonSerializationException)
                    {
                    }
                    catch (JsonReaderException)
                    {
                    }
                }

                return null;
            }

        }
    }

    [Serializable]
    public class Chat_ChildRef
    {
        public string Id { get; set; }
        public int Priority { get; set; }
        public string Condition { get; set; }
        public string FlowName { get; set; }
        public string Method { get; set; }
        public List<Chat_ChildRef> Children { get; set; }
        [DynamoDBProperty("location")]
        public string Location { get; set; }
    }

    [Serializable]
    public class Chat_CarrierMessage
    {
        public string CarrierName { get; set; }
        public string Message { get; set; }
    }

    [Serializable]
    public class RuleData
    {
        public string UserInputType { get; set; }

        // extra fields
        [JsonExtensionData]
        public IDictionary<string, JToken> Properties;
    }
}
