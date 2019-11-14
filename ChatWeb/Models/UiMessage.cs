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

namespace ChatWeb.Models
{
    [Serializable]
    public class UiMessage
    {
        /// <summary>
        /// Type of message.  Ex: question, message, etc.
        /// </summary>
        public string MessageType { get; set; }

        /// <summary>
        /// Plain text message for the user
        /// </summary>
        public string TextMessage { get; set; }
        
        /// <summary>
        /// Custom formatted Ui text.  This text is generally created by Script Actions in the flow
        /// using api.UI().  The format is then controlled by the flow.
        /// </summary>
        public string UiTextMarkup { get; set; }
    }
}