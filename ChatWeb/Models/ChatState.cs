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

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using ChatWeb.Helpers;
using Newtonsoft.Json;

namespace ChatWeb.Models
{
    [Serializable]
    public class ChatState
    {
        public ChatState()
        {
            Messages = new List<ChatMessage>();
            GlobalAnswers = new ChatVariables();
            FlowAnswers = new Dictionary<string, ChatVariables>();
            Steps = new List<ChatFlowStep>();
            FlowStack = new SimpleStack<ChatStackItem>();
            FlowPath = new List<string>();
            PIIAnswers = new List<(PIIType piiType, string text, string mask)>();
            SessionData = new SessionData();

            GlobalAnswers[ChatStandardField.SessionData] = SessionData;
        }

        public List<ChatFlowStep> Steps { get; private set; }
        public List<ChatMessage> Messages { get; private set; }
        public ChatVariables GlobalAnswers { get; private set; }
        public Dictionary<string, ChatVariables> FlowAnswers { get; private set; }
        public List<(PIIType piiType, string text, string mask)> PIIAnswers { get; private set;}

        public SimpleStack<ChatStackItem> FlowStack { get; private set; }
        public List<string> FlowPath { get; private set; }

        public ChannelData ChannelData { get; set; }
        public SessionData SessionData { get; set; }
        public bool TransferToAgent { get; set; }
        public string TransferToAgentSkill { get; set; }

        public IDictionary<string, object> TrustedClientResponse { get; set; }

        public bool DontShowLastMessage { get; set; }

        private ClassificationResults _lastClassification;

        public ClassificationResults LastClassification
        {
            get { return _lastClassification; }
            private set
            {
                _lastClassification = value;
                if (_lastClassification?.TopResults?.Length > 0)
                {
                    GlobalAnswers.FieldAnswers[ChatStandardField.CallReason] = _lastClassification.TopResults[0].Reason;
                    GlobalAnswers.FieldAnswers[ChatStandardField.CallSubreason] = _lastClassification.TopResults[0].Subreason;
                }
                else
                {
                    GlobalAnswers.FieldAnswers[ChatStandardField.CallReason] = "";
                    GlobalAnswers.FieldAnswers[ChatStandardField.CallSubreason] = "";
                }

                SessionData.CallReason = GlobalAnswers.GetFieldAnswer(ChatStandardField.CallReason);
                SessionData.CallSubreason = GlobalAnswers.GetFieldAnswer(ChatStandardField.CallSubreason);

            }
        }

        public ChatVariables GetFlowAnswers(string flowName)
        {
            if (!FlowAnswers.ContainsKey(flowName))
                FlowAnswers.Add(flowName, new ChatVariables());

            return FlowAnswers[flowName];
        }

        public ChatMessage GetLastMessage()
        {
            if (Messages.Count == 0)
                Messages.Add(new ChatMessage());

            return Messages.Last();
        }

        public void AddPIIText(PIIType piiType, string text, string mask)
        {
            PIIAnswers.Add((piiType, text, mask));
        }

        public void AddToFlowPath(string flowName)
        {
            if (!FlowPath.Contains(flowName))
                FlowPath.Add(flowName);
        }

        public void RemoveFlowPath(string flowName)
        {
            if (FlowPath.Contains(flowName))
                FlowPath.Remove(flowName);
        }

        public string[] GetCurrentTranscript()
        {
            var transcript = new List<string>();

            foreach (var message in Messages)
            {
                string avaText = message.BotQuestionsText;
                if (!String.IsNullOrEmpty(avaText))
                    transcript.Add($"Ava: {avaText}");

                if (!String.IsNullOrEmpty(message.UserInput))
                    transcript.Add($"User: {message.UserInput}");
            }

            return transcript.ToArray();
        }

        public int GetUserTimeZoneOffset()
        {
            var userTimeZoneOffset = GlobalAnswers.GetFieldAnswer(ChatStandardField.UsersTimeZoneOffset);
            if (String.IsNullOrEmpty(userTimeZoneOffset))
                return 0;

            if (int.TryParse(userTimeZoneOffset, out int offset))
                return offset;

            return 0;
        }

        public void ChangePartnerContext(string partner, string context)
        {
            GlobalAnswers[ChatStandardField.Partner] = partner;
            GlobalAnswers[ChatStandardField.Context] = context;

            SessionData.Partner = partner;
            SessionData.Context = context;
        }

        public void UpdateLastClassification(ClassificationResults results)
        {
            if (results.IsSuccessful)
                LastClassification = results;
            else
            {
                // Create empty result
                LastClassification = new ClassificationResults(0.0)
                {
                    ClassifierResults = new ClassificationResponse[]
                    { new ClassificationResponse {Intent = null, Probability = -1.0 } }
                };
            }

            GlobalAnswers[ChatStandardField.LastClassification] = LastClassification.GetBestResult();
            SessionData.LastClassification = GlobalAnswers.GetFieldAnswer<ClassificationResponse>(ChatStandardField.LastClassification);
        }

    }
}