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

namespace ChatWeb.Services
{
    public interface IChatScriptManager : IDisposable
    {
        Task Initialize();
        Task<bool> IsConditionMet(ChatModel chatModel, ChatFlowStep chatFlowStep, Chat_ChildRef childRef);
        Task<object> ProcessActions(ChatModel chatModel, ChatFlowStep chatFlowStep, object functionParams, bool expectResult);
        Task<ErrorHandlerResult> ProcessErrorHandler(ChatModel chatModel, ChatFlowStep chatFlowStep);
        ChatScriptHost ScriptHost { get; }
        Task StartChat(ChatModel chatModel);
        Task<object> GetVariable(ChatModel chatModel, string flowName, string variableName);
        Task NotifyFlowEnd(ChatModel chatModel, ChatStackItem chatStackItem);
    }
}
