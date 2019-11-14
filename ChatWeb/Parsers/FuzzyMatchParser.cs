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
using System.Web;
using ChatWeb.Models;
using ChatWeb.Services;
using Newtonsoft.Json;
using ChatWeb.Helpers;

namespace ChatWeb.Parsers
{
    public class FuzzyMatchParser : ChatParserBase
    {
        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static readonly char[] varSplit = new char[] { '.' };

        readonly FuzzyMatchService fuzzyMatchService;
        readonly ChatFlowStep chatFlowStep;
        readonly ChatModel chatModel;
        readonly IChatScriptManager chatScriptManager;

        public FuzzyMatchParser(ChatModel chatModel, FuzzyMatchService fuzzyMatchService, IChatScriptManager chatScriptManager, ChatFlowStep chatFlowStep)
        {
            this.fuzzyMatchService = fuzzyMatchService;
            this.chatFlowStep = chatFlowStep;
            this.chatModel = chatModel;
            this.chatScriptManager = chatScriptManager;
        }
        public override async Task<ParseResult> ParseAsync(ChatState state, Chat_ParseField chatParseField, ChatMessage message)
        {
            int matchThreshold = ChatConfiguration.FuzzyNameMatchThreshold;
            if (!String.IsNullOrEmpty(chatParseField.RuleData))
                matchThreshold = Convert.ToInt32(chatParseField.RuleData);

            FuzzyMatchParserData[] possibleMatches = await GetPossibleMatches(state, chatFlowStep, chatParseField);

            if (UtilityMethods.IsNullOrEmpty(possibleMatches))
            {
                logger.WarnFormat("Parse: Invalid field data for fuzzy match. {0} - {1}", chatParseField.FieldName, chatParseField.SourceData);
                return ParseResult.Failed;
            }

            // Get list of test values for API
            var testVals = possibleMatches.Select(x => x.text).ToArray();
            var response = await fuzzyMatchService.Match(GetTextSource(chatParseField, message), testVals);

            if (response == null)
            {
                logger.Error("Network: Failed to call fuzzy match service.");
                return ParseResult.Failed;
            }

            if (response.Output?.Length > 0)
            {
                IEnumerable<FuzzyMatchParserResult> matches = FindMatches(matchThreshold, possibleMatches, response);

                if (matches.Count() > 0)
                    return ParseResult.CreateSuccess(matches.First());
            }
            else
            {
                logger.Debug("Parse: No fuzzy matches found.");
            }

            // Always return true that we processed the text even if we didnt find a matching name.
            return ParseResult.CreateSuccess(null);
        }

        private static IEnumerable<FuzzyMatchParserResult> FindMatches(int matchThreshold, FuzzyMatchParserData[] possibleMatches, FuzzyMatchResponse response)
        {
            return (from val in possibleMatches
                    join r in response.Output on val.text equals r.Candidate
                    where (r.Score >= (val.threshold == 0 ? matchThreshold : val.threshold))
                    select new FuzzyMatchParserResult
                    {
                        text = val.text,
                        match = r.Match,
                        score = r.Score,
                        threshold = val.threshold == 0 ? matchThreshold : val.threshold,
                        value = val.value
                    });
        }

        private async Task<FuzzyMatchParserData[]> GetPossibleMatches(ChatState state, ChatFlowStep chatFlowStep, Chat_ParseField chatParseField)
        {
            FuzzyMatchParserData[] possibleMatches = null;

            var data = await chatScriptManager.GetVariable(chatModel, chatFlowStep.Flow, chatParseField.SourceData);

            if (data == null)
            {
                // Direct string array entered in source data
                possibleMatches = (from rd in chatParseField.SourceData.Split(',')
                                   select new FuzzyMatchParserData { text = rd, value = rd }).ToArray();
            }
            else if (data is string stringData)
            {
                // Rule data variable set to just a string
                possibleMatches = (from rd in ((string)stringData).Split(',')
                                   select new FuzzyMatchParserData { text = rd, value = rd }).ToArray();
            }
            else if (data is Newtonsoft.Json.Linq.JValue valueData)
            {
                // Rule data variable set to just a string
                possibleMatches = (from rd in ((string)valueData.Value).Split(',')
                                   select new FuzzyMatchParserData { text = rd, value = rd }).ToArray();
            }
            else
            {
                // Convert script dictionary array to actual object
                var x = JsonConvert.SerializeObject(data);

                // TODO: Detect deserialization errors in case script gives bad data.
                // We want a nice error back in the response so its easier to figure out.
                possibleMatches = JsonConvert.DeserializeObject<FuzzyMatchParserData[]>(x);
                possibleMatches = possibleMatches.Where(m => !String.IsNullOrEmpty(m.text)).ToArray();
            }

            return possibleMatches;
        }

        public override void UpdateState(ChatState state, ChatFlowStep flowStep, Chat_ParseField chatParseField, ParseResult result)
        {
            var variables = GetChatVariables(state, flowStep, chatParseField.VarScope);

            if (result.Success && (result.Answer is FuzzyMatchParserResult answer))
            {
                // Convert back to Dictionary for proper formatting, and script access
                var x = JsonConvert.SerializeObject(answer);
                variables[chatParseField.FieldName + "_MatchInfo"] = JsonConvert.DeserializeObject<Dictionary<string, object>>(x);
                variables[chatParseField.FieldName] = true;

                // Send both user's input and matched input as PII.
                CheckIfAnswerHasPII(state, chatParseField, answer.match.ToString(), PIIMask);
                CheckIfAnswerHasPII(state, chatParseField, answer.text.ToString(), PIIMask);
            }
            else
            {
                variables[chatParseField.FieldName] = false;
            }
        }
    }
}