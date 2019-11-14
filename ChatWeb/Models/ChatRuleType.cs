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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatWeb.Models
{
   
    public class ChatRuleType
    {
        public const string FuzzyMatch = "fuzzymatch";
        public const string NameParseNoHistory = "nameparsenohistory";
        public const string NameParse = "nameparse";
        public const string PhoneNumber = "phonenumber";
        public const string PhoneNumberNoHistory = "phonenumbernohistory";
        public const string Email = "email";
        public const string Regex = "regex";
        public const string Fallback = "fallback";
        public const string Parse = "parse";
        public const string ParseDevice = "parsedevice";
        public const string ParseBackupProvider = "parsebackupprovider";
        public const string ParseCarrier = "parsecarrier";
        public const string ParseRetailPartner = "parseretailpartner";
        public const string IntentGatewayParser = "intentgatewayparser";
        public const string PauseParser = "pauseparser";
        public const string YesNoParser = "yesnoparser";
        public const string DontKnowParser = "dontknowparser";
        public const string ContinueParser = "continueparser";
        public const string DateParser = "dateparser";
        public const string DateParserV2 = "dateparserv2";
        public const string TimeParser = "timeparser";
        public const string LossCategoryParser = "losscategoryparser";
        public const string LossCategoryParserOptions = "losscategoryparseroptions";
        public const string AppNameParser = "appnameparser";
        public const string BluetoothDeviceParser = "bluetoothdeviceparser";
        public const string AddressParser = "addressparser";
        public const string ZipCodeParser = "zipcode";
        public const string ColorParser = "colorparser";
        public const string NumberParser = "numberparser";
    }

    public class ChatSource
    {
        public const string AccountDevice = "accountDevice";
        public const string CorrectedInput = "correctedInput";
        public const string OriginalInput = "originalInput";
        public const string SelectedDevice = "selectedDevice";
    }

    public class ChatStandardField
    {
        public const string SessionData = "SessionData";
        public const string SessionId = "SessionId";
        public const string Name = "Name";
        public const string PhoneNumber = "PhoneNumber";
        public const string AgentName = "AgentName";
        public const string Partner = "Partner";
        public const string Context = "Context";
        public const string Channel = "Channel";
        public const string InputType = "InputType";
        public const string DelayPrompt = "DelayPrompt";
        public const string UserAskedWhy = "UserAskedWhy";
        public const string CallReason = "CallReason";
        public const string CallSubreason = "CallSubreason";
        public const string Environment = "Environment";
        public const string UsersTimeZoneOffset = "UsersTimeZoneOffset";
        public const string EstimatedLiveAgentWaitTime = "EstimatedLiveAgentWaitTime";
        public const string EstimatedLiveAgentWaitTimes = "EstimatedLiveAgentWaitTimes";
        public const string ClientData = "ClientData";
        public const string TrustedClientData = "TrustedClientData";
        public const string ClaimsPod = "ClaimsPod";
        public const string UtmSource = "UtmSource";
        public const string UtmMedium = "UtmMedium";
        public const string UtmCampaign = "UtmCampaign";
        public const string DistinctId = "DistinctId";
        public const string Fingerprint = "Fingerprint";
        public const string LastUserInput = "LastUserInput";
        public const string LastCorrectedUserInput = "LastCorrectedUserInput";
        public const string LastClassification = "LastClassification";
        public const string TransferToAgent = "TransferToAgent";
        public const string UnsupportedIntent = "UnsupportedIntent";
        public const string AutoFill = "autoFill";
        public const string IsNodeScript = "IsNodeScript";
    }

    public class ChatObjectType
    {
        //public const string Question = "Question";
        public const string Message = "Message";
        public const string StaticMessage = "StaticMessage";
        public const string Analyze = "Analyze";
        //public const string Action = "Action";
        public const string Connection = "Connection";
        public const string UIElement = "UIElement";
        public const string Instruction = "Instruction";
    }

    public enum ChatSubType
    {
        None = 0,
        Debug = 1
    }
}

