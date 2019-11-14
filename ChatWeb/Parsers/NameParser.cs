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
using ChatWeb.Models;
using System.Globalization;
using System.Threading;
using ChatWeb.Services;

namespace ChatWeb.Parsers
{
    public class NameParser : ChatParserBase
    {
        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        readonly TextParserService textParserService;
        const string TestName = "Customer US";
        
        // http://www.nancy.cc/2-letter-boy-names/
        // http://www.nancy.cc/2-letter-girl-names/
        // Sample taken from above site.  not all are added since they may conflict with common words
        readonly string TwoLetterNamesRegex = @"\b((ab)|(al)|(bo)|(cy)|(ed)|(le)|(ty)|(jo)|(lu)|(jd)|(jt)|(jc)|(jp)|(jr)|(aj)|(bj)|(dj)|(rj)|(tj))\b";


        public NameParser(TextParserService textParserService)
        {
            this.textParserService = textParserService;
            this.PIIMask = "NAME";
            this.PIIType = PIIType.Low;
        }

        public override async Task<ParseResult> ParseAsync(ChatState state, Chat_ParseField chatParseField, ChatMessage message)
        {
            string agentName = state.GlobalAnswers.GetFieldAnswer<string>(ChatStandardField.AgentName);

            (var names, var tokens) = await GetNamesAndTokens(message.UserInput, agentName);

            if (tokens == null)
            {
                logger.Error("Network: Failed to call text parser service.");
                return ParseResult.Failed;
            }

            // If we didnt detect any names, and the user entered only 2 or 3 words (possible names),
            // and no look behind (to cut down on false positives), we retry with "My name is " prepended
            // since the NLP is better at picking up names in a sentence.
            if ((names.Count == 0) && (tokens.Count <= 3) && (!chatParseField.CheckPreviousMessages))
                (names, tokens) = await GetNamesAndTokens("My name is " + message.UserInput, agentName);

            names.AddRange(GetUndetectedNames(message.UserInput));

            // Add our test name if detected
            if (message.UserInput.ToUpper().Contains(TestName.ToUpper()))
                names.AddRange(TestName.Split(' '));

            if (names.Count > 0)
                return ParseResult.CreateSuccess(names.Distinct().ToArray());

            return ParseResult.Failed;
        }

        async Task<(List<string> names, List<TextParserToken> tokens)> GetNamesAndTokens(string text, string filterName)
        {
            var response = await textParserService.Parse(text);

            if (response == null)
                return (null, null);

            var tokens = (from sentence in response.Sentences
                    from token in sentence.Tokens
                    select token).ToList();

            var names = (
                from token in tokens
                where token.Ner == "PERSON" && !token.Word.Equals(filterName, StringComparison.OrdinalIgnoreCase)
                select token.Word
            ).ToList();

            return (names, tokens);
        }

        IEnumerable<string> GetUndetectedNames(string text)
        {
            var match = System.Text.RegularExpressions.Regex.Match(text, TwoLetterNamesRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
                return new string[] { match.Value };

            return new string[] { };
        }


        public override void UpdateState(ChatState state, ChatFlowStep flowStep, Chat_ParseField chatParseField, ParseResult result)
        {
            if (result.Success)
            {
                string[] names = result.Answer as string[];
                var variables = GetChatVariables(state, flowStep, chatParseField.VarScope);

                // Set subvariables for first and last name
                if (names.Length > 0)
                {
                    variables[$"{chatParseField.FieldName}_First"] = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(names[0]);
                    CheckIfAnswerHasPII(state, PIIType, names[0], PIIMask);
                }

                if (names.Length > 1)
                {
                    variables[$"{chatParseField.FieldName}_Last"] = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(names.Last());
                    CheckIfAnswerHasPII(state, PIIType, names[1], PIIMask);
                }

                // TODO: Use parse dependencies to join them properly.  They will be marked as "compound".
                variables[chatParseField.FieldName] = String.Join(" ", names);
            }
        }
    }
}