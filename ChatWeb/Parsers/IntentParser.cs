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

namespace ChatWeb.Parsers
{
    public class IntentParser : ChatParserBase
    {
        readonly string defaultClassifier;
        readonly TextClassificationService classificationService;
        readonly UserDataFilterService filterService;
        readonly double threshold;

        public IntentParser(TextClassificationService classificationService, UserDataFilterService filterService, double threshold, string defaultClassifier)
        {
            this.defaultClassifier = defaultClassifier;
            this.classificationService = classificationService;
            this.filterService = filterService;
            this.threshold = threshold;
        }

        public override async Task<ParseResult> ParseAsync(ChatState chatState, Chat_ParseField chatParseField, ChatMessage message)
        {
            // Strip PII data
            string text = filterService.FilterUserData(chatState, message.CorrectedUserInput, false);
            string classifiers = chatParseField?.GetProperty("Classifiers") ?? defaultClassifier;

            message.Classifications = await classificationService.ClassifyAsync(classifiers, threshold, text, false, chatState.SessionData.IsSmsChannel);
            chatState.UpdateLastClassification(message.Classifications);

            if (message.Classifications.IsSuccessful)
            {
                // We don't want common chat intent's here.  
                if (message.Classifications.GetBestResult().Intent.StartsWith("commonchat-"))
                    return ParseResult.Failed;

                return ParseResult.CreateSuccess(message.Classifications.GetBestResult().Intent);
            }

            return ParseResult.Failed;
        }
    }
}