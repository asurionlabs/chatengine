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

using ChatWeb.Models;
using System;
using System.Text;

namespace ChatWeb
{
    public class ChatConfiguration
    {
        public static readonly string[] AlwaysAllowedFlows = { "commonchat.*", "TransferAgent.*", "FailToUnderstand.*" };
        public const int OldWebClientConnectionLimit = 1000;
        public const int MaximumWebServiceCallRetries = 5;
        public const double MinimumConfidenceRatioUnknownClassification = .50;
        public const double MininimConfidenceRatio = 0.55;
        public const double MinimumLuisCommonChatConfidenceRatio = 0.50;
        public const double MinimumDeviceConfidence = .75;
        public const double MinimumUncommonDeviceConfidence = .80;
        public const int ClassificationCacheTimeoutSeconds = 24 * 60 * 60;  // 24 hours
        public const int MaximumStepCount = 100;
        public const int MaximumConsecutiveBadMessages = 2;
        public const int MaximumTotalBadMessages = 10;
        public const int MaximumChatTimeoutMinutes = 120;
        public const int MaximumSmsChatTimeoutMinutes = 10080;
        public const int DefaultChatTimeout = 60;
        public const int DefaultSmsChatTimeoutMinutes = 10080;
        public const int MaximumMessageBackSearch = 5;
        public const int FuzzyNameMatchThreshold = 85;
        public const int MaximumSimpleContinueInputLength = 10;
        public const double MinLuisConfidenceRatio = 0.20;
        public const int MinimumBadPhoneDigitCount = 6;
        public const int MaximumBadPhoneDigitCount = 12;
        public const double MinimumColorMatchRatio = .85;
        public const int MaximumTextInput = 350;
        public static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(3);
        public const int SpellCheckTimeout = 10000;   // milliseconds
        public const int StsCredentialExpiration = 900; //43200;  // 12 hours is max time
        public const int DefaultRestAPIWarnSendTime = 15;
        public const int ChatResponseWarnMaxTime = 55;
        public const string DefaultServerErrorMessage = "Something's not working right.\n\nGive me a few moments to get my system up and running and then try typing it again.";
        public const string AnywhereExpertServerErrorMessage = "Ehh, something's not working right. Let me connect you with an expert from our team. They'll be with you shortly...";

        public const bool DisableXRayTracing = false;

        public const int RegexCacheSize = 50;  // System default is 15.  We increase this since we rely on regex a lot.

        public const int TimeZoneOffset = -6;   // Default to CST if the user doesn't give it to us

        public UrlConfig AddressParse { get; set; }
        public bool AllowDebugMode { get; set; }
        public string AwsTablePrefix { get; set; }
        public string ChatEnvironmentName { get; set; }
        public string ClassificationMemCacheConfigNode { get; set; }
        public string ClassifierConfigFile { get; set; }
        public string CloudWatchLogGroup { get; set; }
        public string DebugTablePrefix { get; set; }
        public UrlConfig FuzzyMatch { get; set; }
        public UrlConfig IntentGateway { get; set; }
        public string KibanaLogUrl { get; set; }
        public KinesisConfig KinesisAnalytics { get; set; }
        public KinesisConfig KinesisLog { get; set; }
        public string LogLevel { get; set; }
        public string Log4NetConfigFile { get; set; }
        public UrlConfig LuisDamageClassifier { get; set; }
        public string LuisSpellCheckerKey { get; set; }
        public string MemCacheConfigNode { get; set; }
        public bool ShowDebugChatMessages { get; set; }
        public SmtpConfig Smtp { get; set; }
        public UrlConfig SpellCheckService { get; set; }
        public UrlConfig TextParser { get; set; }
        public string TrustedClientKey { get; set; }

        public string GetConfigurationSettings()
        {
            var configLog = new StringBuilder();
            
            configLog.Append("Config: \n");
            configLog.AppendFormat("{0} = {1}\n", nameof(AwsTablePrefix), AwsTablePrefix);
            configLog.AppendFormat("{0} = {1}\n", nameof(AddressParse), AddressParse.Url);
            configLog.AppendFormat("{0} = {1}\n", nameof(AllowDebugMode), AllowDebugMode);
            configLog.AppendFormat("{0} = {1}\n", nameof(ChatEnvironmentName), ChatEnvironmentName);
            configLog.AppendFormat("{0} = {1}\n", nameof(ClassificationMemCacheConfigNode), ClassificationMemCacheConfigNode);
            configLog.AppendFormat("{0} = {1}\n", nameof(ClassifierConfigFile), ClassifierConfigFile);
            configLog.AppendFormat("{0} = {1}\n", nameof(CloudWatchLogGroup), CloudWatchLogGroup);
            configLog.AppendFormat("{0} = {1}\n", nameof(DebugTablePrefix), DebugTablePrefix);
            configLog.AppendFormat("{0} = {1}\n", nameof(FuzzyMatch), FuzzyMatch.Url);
            configLog.AppendFormat("{0} = {1}\n", nameof(IntentGateway), IntentGateway.Url);
            configLog.AppendFormat("{0} = {1}\n", nameof(KibanaLogUrl), KibanaLogUrl);
            configLog.AppendFormat("{0} = {1}\n", nameof(KinesisAnalytics), $"StreamName: {KinesisAnalytics.StreamName}, Arn: {KinesisAnalytics.Arn}, StsRoleSessionName: {KinesisAnalytics.StsRoleSessionName}");
            configLog.AppendFormat("{0} = {1}\n", nameof(KinesisLog), $"StreamName: {KinesisLog.StreamName}, Arn: {KinesisLog.Arn}, StsRoleSessionName: {KinesisLog.StsRoleSessionName}");
            configLog.AppendFormat("{0} = {1}\n", nameof(LogLevel), LogLevel);
            configLog.AppendFormat("{0} = {1}\n", nameof(Log4NetConfigFile), Log4NetConfigFile);
            configLog.AppendFormat("{0} = {1}\n", nameof(LuisDamageClassifier), LuisDamageClassifier.Url);
            configLog.AppendFormat("{0} = {1}\n", nameof(MemCacheConfigNode), MemCacheConfigNode);
            configLog.AppendFormat("{0} = {1}\n", nameof(ShowDebugChatMessages), ShowDebugChatMessages);
            configLog.AppendFormat("{0} = {1}\n", nameof(Smtp), $"From: {Smtp.From}, Server: {Smtp.Server}:{Smtp.Port}");
            configLog.AppendFormat("{0} = {1}\n", nameof(SpellCheckService), SpellCheckService.Url);
            configLog.AppendFormat("{0} = {1}\n", nameof(TextParser), TextParser.Url);

            return configLog.ToString();
        }
    }
}