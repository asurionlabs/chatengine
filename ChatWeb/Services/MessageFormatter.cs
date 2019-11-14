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

using ChatWeb.Helpers;
using ChatWeb.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace ChatWeb.Services
{
    public class MessageFormatter
    {
        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static readonly string variableRegex = @"(((?<scope>s|global|local)(?=(\[""|\.)))?((\[""?|\.)?(?<varlevel>[^""\[\]\.%@]+)(\""?])?)+)";
        static readonly string messageRegex = @"[%@](?<group>&?)" + variableRegex + "[%@]";
        static readonly string htmlStartTagRegex = @"< ?(?<tag>br|li|ul) ?/? ?>";
        static readonly string htmlEndTagRegex = @"< ?/ ?(?<tag>br|li|ul) ?>";
        static readonly string htmlStartEndTagRegex = @"< ?/? ?(?<tag>b|i) ?>";

        static readonly string regexStripHtml = "(<a .*?href ?= ?[\"'](?<url>[^\"']*?)[\"'](?<s>.*?</a>))";

        private MessageFormatter() { }

        public static string FormatMessage(ChatState state, ChatFlowStep step)
        {
            string text = null;

            // Check for carrier override text
            if (step.CarrierMessages != null)
            {
                string partner = state.GlobalAnswers.GetFieldAnswer<string>(ChatStandardField.Partner);
                text = (from m in step.CarrierMessages
                        where m.CarrierName == partner
                        select m.Message).FirstOrDefault();
            }

            if (String.IsNullOrEmpty(text))
                text = step.GetText();

            return FormatMessage(state, step.Flow, text);
        }

        public static string FormatMessage(ChatState state, string flowName, string text)
        {
            if (String.IsNullOrEmpty(text))
                return text;

            StringBuilder sbQuestion = new StringBuilder(text);

            // Support these:  
            // %s.test% 
            // %flow.test% 
            // %s["test/test"].test.parm["xyz"]%
            // %s["test/test"].test2[0]%
            // %s["test/test"]%   - single layer with brackets
            // %testvar%
            // %session%  - note starts with s
            // @s.test@  - old syntax
            // %&s.list%   - list format with "and"
            var matches = Regex.Matches(text, messageRegex, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                object value = ResolveVariable(match, state, flowName);
                string groupType = match.Groups["group"].Value;
                string variableText = match.Groups[0].Value;

                string combineText = "or";
                if (groupType == "&")
                    combineText = "and";

                if (value == null)
                    continue;
                else if (value is string stringValue)
                    sbQuestion.Replace(variableText, stringValue);
                else if (value is List<string> listValue)
                    sbQuestion.Replace(variableText, FormatTextList((listValue).ToArray(), combineText));
                else if (value is string[] stringArrayValue)
                    sbQuestion.Replace(variableText, FormatTextList(stringArrayValue, combineText));
                else if (value is object[] objectArrayValue)
                    sbQuestion.Replace(variableText, FormatTextList(objectArrayValue, combineText));
                else if (value is JArray jarrayValue)
                    sbQuestion.Replace(variableText, FormatTextList((jarrayValue).ToObject<string[]>(), combineText));
                else
                    sbQuestion.Replace(variableText, value.ToString());
            }

            if (state.SessionData.Channel == "aep_app")
                return ConvertHtmlToMarkdown(sbQuestion.ToString());

            return sbQuestion.ToString();
        }

        static string ConvertHtmlToMarkdown(string text)
        {
            // Remove unnecessary end tags
            var markdown = Regex.Replace(text, htmlEndTagRegex, "", RegexOptions.Compiled);

            // Convert start/end tags as needed
            var matches = Regex.Matches(text, htmlStartEndTagRegex, RegexOptions.Compiled);
            foreach (Match match in matches)
            {
                var replaceText = "";
                switch (match.Groups["tag"].Value)
                {
                    case "b": replaceText = "*"; break;
                    case "i": replaceText = "_"; break;
                }

                markdown = markdown.Replace(match.Value, replaceText);
            }

            // Convert standalone start tags
            matches = Regex.Matches(text, htmlStartTagRegex, RegexOptions.Compiled);
            foreach (Match match in matches)
            {
                var replaceText = "";
                switch (match.Groups["tag"].Value)
                {
                    case "br": replaceText = "\n"; break;
                }

                markdown = markdown.Replace(match.Value, replaceText);
            }

            return Regex.Replace(markdown, regexStripHtml, "${url}", RegexOptions.Compiled);
        }

        public static object ResolveVariable(ChatState state, string flowName, string text)
        {
            if (String.IsNullOrEmpty(text))
                return text;

            var match = Regex.Match(text, variableRegex, RegexOptions.IgnoreCase);
            return ResolveVariable(match, state, flowName);
        }

        private static object ResolveVariable(Match match, ChatState state, string flowName)
        {
            string scope = match.Groups["scope"].Value;
            string variableText = match.Groups[0].Value;
            string groupType = match.Groups["group"].Value;

            var variableLevels = match.Groups["varlevel"].Captures;
            string variableBase = variableLevels[0].Value;

            object value = null;
            if (scope == "local")
            {
                var flowAnswers = state.GetFlowAnswers(flowName);
                value = flowAnswers[variableBase];
            }
            else
                value = state.GlobalAnswers[variableBase];

            if (value == null)
                return value;

            if (!UtilityMethods.IsValueType(value) && (variableLevels.Count > 1))
            {
                var levels = variableLevels
                    .Cast<Capture>()
                    .Select(m => m.Value)
                    .ToArray();

                try
                {
                    value = FormatMultiLevel(levels, value);
                }
                catch (KeyNotFoundException)
                {
                    logger.WarnFormat("Flow: Message text variable not set.  {0}", variableText);
                    value = null;
                }
            }

            return value;
        }

        private static object FormatMultiLevel(string[] levels, object value)
        {
            dynamic dynValue = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(value));

            for (int i = 1; i < levels.Length; i++)
            {
                var level = levels[i];

                // if object is array, use int type dereferencing,
                // else use dictionary type dereferencing
                if ((dynValue is Array) && (int.TryParse(level, out var levelNum)))
                    dynValue = dynValue[levelNum];
                else
                    dynValue = dynValue[level];
            }

            return dynValue;
        }

        /// <summary>
        /// Formats a list of items to be understandable to a user
        /// </summary>
        /// <returns></returns>
        static string FormatTextList(object[] list, string combineText)
        {
            StringBuilder sb = new StringBuilder();

            if (list != null)
            {
                for (int i = 0; i < list.Length; i++)
                {
                    var item = list[i].ToString();

                    if ((list.Length > 1) && (i == list.Length - 1))
                    {
                        sb.Append(" ");
                        sb.Append(combineText);
                        sb.Append(" ");
                    }
                    else if (sb.Length > 0)
                        sb.Append(", ");

                    sb.Append(item);
                }
            }

            return sb.ToString();
        }
    }
}