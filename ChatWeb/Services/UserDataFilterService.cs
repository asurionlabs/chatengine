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
using ChatWeb.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace ChatWeb.Services
{
    public class UserDataFilterService
    {
        /// <summary>
        /// Strips PII data from a string
        /// </summary>
        /// <param name="chatState">Current chat state.  this is used to find detected PII data</param>
        /// <param name="text">Text to clean</param>
        /// <param name="mask">Flag to indicate if the text should be masked or completely stripped.</param>
        /// <returns></returns>
        public string FilterUserData(ChatState chatState, string text, bool mask = true)
        {
            return FilterUserInfo(chatState, text, mask, true);
        }

        public string FilterHighRiskPII(ChatState chatState, string text, bool mask = true)
        {
            return FilterUserInfo(chatState, text, mask, false);
        }

        private string FilterUserInfo(ChatState chatState, string text, bool mask, bool allPII)
        {
            if (String.IsNullOrWhiteSpace(text))
                return text;

            var replaceText = new StringBuilder(text);

            if (allPII)
            {
                // Clean all phone numbers
                RegexReplace(replaceText, PhoneNumberParser.PhoneNumberRegex, mask ? "<PHONE>" : String.Empty);

                // Clean all email address
                RegexReplace(replaceText, EmailParser.PartialEmailRegex, mask ? "<EMAIL>" : String.Empty);
            }

            if (chatState?.PIIAnswers.Count > 0)
            {
                foreach ((var piiType, var piiText, var piiMask) in chatState.PIIAnswers)
                {
                    if (allPII || (piiType == PIIType.High))
                        StringReplace(replaceText, piiText, mask ? "<" + piiMask + ">" : String.Empty);
                }
            }

            return replaceText.ToString();
        }

        private static void StringReplaceOld(StringBuilder text, string oldValue, string newValue)
        {
            // TODO: Find more efficient StringBuilder string replace to avoid creating many immutable strings (ToString)
            int index = 0;

            while (index >= 0)
            {
                string replacedText = text.ToString();
                index = replacedText.IndexOf(oldValue, StringComparison.InvariantCultureIgnoreCase);
                if (index == -1)
                    return;

                string foundText = replacedText.Substring(index, oldValue.Length);
                text.Replace(foundText, newValue);
            }
        }

        private static void StringReplace(StringBuilder text, string oldValue, string newValue)
        {
            if (oldValue == null || oldValue.Length < 3)
                return;

            // TODO: Find more efficient StringBuilder string replace to avoid creating many immutable strings (ToString)
            int index = 0;

            while ((index >= 0) && (index < text.Length))
            {
                string replacedText = text.ToString();
                index = replacedText.IndexOf(oldValue, index, StringComparison.InvariantCultureIgnoreCase);
                if (index == -1)
                    return;

                string foundText = replacedText.Substring(index, oldValue.Length);
                text.Replace(foundText, newValue, index, oldValue.Length);
                index += newValue.Length;
            }
        }

        /// <summary>
        /// Uses regex to filter text without filtering the mask text itself.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        private static void MaskReplace(StringBuilder text, string oldValue, string newValue)
        {
            if (oldValue.Length < 3)
                return;

            string mask = String.Format("(?:<.*>)|(?<found>{0})", Regex.Escape(oldValue));

            // TODO: Make more efficient since Regex doesn't work with StringBuilder, so we are doing a ToString() here which causes immutable string creation.
            var matches = Regex.Matches(text.ToString(), mask, RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                if (match.Groups["found"].Success)
                    text.Replace(match.Groups["found"].Value, newValue);
            }
        }

        private static void RegexReplace(StringBuilder text, string regex, string replaceText)
        {
            // TODO: Make more efficient since Regex doesn't work with StringBuilder, so we are doing a ToString() here which causes immutable string creation.
            var match = Regex.Match(text.ToString(), regex, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                text.Replace(match.Groups[1].Value, replaceText);
                //match = match.NextMatch();
            }
        }
    }
}