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
using System.Threading.Tasks;

namespace ChatWeb.Models
{
    [Serializable]
    public class SessionData
    {
        public string ChatId { get; set; }
        public string SessionId { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string AgentName { get; set; }
        public string Partner { get; set; }
        public string Context { get; set; }
        public string Channel { get; set; }
        public string InputType { get; set; }
        public string DelayPrompt { get; set; }
        public bool DummyMode { get; set; }
        public bool UserAskedWhy { get; set; }
        public string CallReason { get; set; }
        public string CallSubreason { get; set; }
        public Dictionary<string, object> Environment { get; set; }
        public string UsersTimeZoneOffset { get; set; }
        public int EstimatedLiveAgentWaitTime { get; set; }
        public Dictionary<string, int> EstimatedLiveAgentWaitTimes { get; set; }
        public string UtmSource { get; set; }
        public string UtmMedium { get; set; }
        public string UtmCampaign { get; set; }
        public string DistinctId { get; set; }
        public string Fingerprint { get; set; }
        public string LastUserInput { get; set; }
        public string LastCorrectedUserInput { get; set; }
        public ClassificationResponse LastClassification { get; set; }
        public bool TransferToAgent { get; set; }
        public ClassificationResponse UnsupportedIntent { get; set; }
        public ErrorHandlerData ErrorData { get; set; }

        public string PartnerContext
        {
            get { return $"{Partner}-{Context}"; }
        }

        public bool IsSmsChannel
        {
            get { return Channel == "sms"; }
        }

    }
}
