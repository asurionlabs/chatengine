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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace ChatWeb.Parsers
{
    public abstract class ChatParserBase
    {
        public virtual string GetTextSource(Chat_ParseField chatParseField, ChatMessage chatMessage)
        {
            if (chatParseField.SourceData == ChatSource.CorrectedInput)
                return chatMessage.CorrectedUserInput?.Trim();

            return chatMessage.UserInput?.Trim();
        }

        public string PIIMask = "PII";
        public PIIType PIIType = PIIType.None;

        protected ChatVariables GetChatVariables(ChatState state, ChatFlowStep flowStep, VariableScope scope)
        {
            return scope == VariableScope.Local ? state.GetFlowAnswers(flowStep.Flow) : state.GlobalAnswers;
        }

        public virtual Task<ParseResult> ParseAsync(ChatState state, Chat_ParseField chatParseField, ChatMessage message)
        {
            throw new NotImplementedException();
        }

        public virtual void UpdateState(ChatState state, ChatFlowStep flowStep, Chat_ParseField chatParseField, ParseResult result)
        {
            if (result.Success)
            {
                GetChatVariables(state, flowStep, chatParseField.VarScope)[chatParseField.FieldName] = result.Answer;

                CheckIfAnswerHasPII(state, chatParseField, result.Answer.ToString(), PIIMask);
            }
        }

        protected virtual void CheckIfAnswerHasPII(ChatState state, Chat_ParseField chatParseField, string answer, string mask)
        {
            var piiType = chatParseField.PIIType;
            if (piiType == PIIType.None)
                piiType = PIIType;
                
            CheckIfAnswerHasPII(state, piiType, answer, mask);
        }

        protected virtual void CheckIfAnswerHasPII(ChatState state, PIIType piiType, string answer, string mask)
        {
            if ((piiType != PIIType.None) && !String.IsNullOrEmpty(answer))
                state.AddPIIText(piiType, answer, mask);
        }
    }
}