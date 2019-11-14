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
using System.Web;

namespace ChatWeb.Services
{
    public class MatchService
    {
        public static (string matchedText, int matchedIndex, float ratio) FindMatch(string matchText, int minTokenCount, int maxTokenCount, char[][] sourceList)
        {
            var tokens = TextClassificationService.Tokenize(matchText);
            return FindMatch(tokens, minTokenCount, maxTokenCount, sourceList);
        }

        public static (string matchedText, int matchedIndex, float ratio) FindMatch(string[] tokens, int minTokenCount, int maxTokenCount, char[][] sourceList)
        {
            bool fullMatch = false;
            var resultList = new List<(string text, int matchedIndex, float ratio)>();
            var tokenCount = tokens.Count();

            // Loop through all tokens
            for (int tokenIndex = 0; tokenIndex < tokenCount && !fullMatch; tokenIndex++)
            {
                // Loop through Descending N-Grams
                for (int numberGrams = maxTokenCount; numberGrams >= minTokenCount && !fullMatch; numberGrams--)
                {
                    if (tokenIndex + numberGrams > tokenCount)
                        continue;

                    string nGram = String.Join(" ", tokens, tokenIndex, numberGrams);
                    var result = MatchTokens(nGram, sourceList);
                    resultList.Add(result);
                    if (result.ratio == 1)
                        fullMatch = true;
                }
            }

            // sort by ratio and return the highest
            return (from d in resultList
                    orderby d.ratio descending
                    select d).FirstOrDefault();
        }

        static (string text, int matchedIndex, float ratio) MatchTokens(string matchText, char[][] sourceList)
        {
            var ratiolist = new List<float>();
            var testLen = matchText.Length;
            var testChars = matchText.ToCharArray();
            for (int i = 0; i < sourceList.Length; i++)
            {
                var makeModel = sourceList[i];

                float lensum = (float)(testLen + makeModel.Length);
                float levdist = (float)Levenshtein.LevenshteinDistance(testChars, makeModel);
                var ratio = ((lensum - levdist) / lensum);
                ratiolist.Add(ratio);
            }

            var make_value = ratiolist.Max();
            var make_index = ratiolist.IndexOf(make_value);

            return (matchText, make_index, make_value);
        }
    }
}