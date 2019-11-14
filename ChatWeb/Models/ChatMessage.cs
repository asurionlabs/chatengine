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

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatWeb.Models
{
    /// <summary>
    /// Chat message information
    /// </summary>
    [Serializable]
    public class ChatMessage
    {
        public string UserInput { get; set; }
        public string CorrectedUserInput { get; set; }
        public LuisResponse LuisDateOutput { get; set; }
        public LuisResponse LuisDamageClassifierOutput { get; set; }
        public string PlaceholderText { get; set; }
        public List<ChatStepId> Steps { get; set; }
        public ClassificationResults Classifications { get; set; }

        /// <summary>
        /// Text response from the server for display to end-user
        /// </summary>
        List<UiMessage> _BotQuestions { get; set; }

        public ChatMessage()
        {
            _BotQuestions = new List<UiMessage>();
            Steps = new List<ChatStepId>();
        }

        public void AddUiText(string type, string text, string UiText)
        {
            type = RemapLegacyMessageType(type);

            if (!String.IsNullOrWhiteSpace(UiText) || !String.IsNullOrWhiteSpace(text))
                _BotQuestions.Add(new UiMessage() { MessageType = type, TextMessage = text, UiTextMarkup = UiText });
        }

        private static string RemapLegacyMessageType(string type)
        {
            if (type == "Analyze")
                type = "Action";
            else if (type == "Message")
                type = "Question";
            else if (type == "StaticMessage")
                type = "Message";
            return type;
        }

        public string[] BotQuestionsTextList
        {
            get { return _BotQuestions.Select(b => b.TextMessage).Where(x => !String.IsNullOrEmpty(x)).ToArray(); }
        }

        public string BotQuestionsText
        {
            get { return String.Join(" ", BotQuestionsTextList); }
        }

        public UiMessage[] BotQuestions
        {
            get { return _BotQuestions.ToArray(); }
        }
    }
}
