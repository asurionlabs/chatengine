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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ChatWeb.Models
{
    [Serializable]
    public class ChatVariables
    {
        public ChatVariables()
        {
            FieldAnswers = new Dictionary<string, object>();
        }

        public object this[string fieldName]
        {
            get { return GetFieldAnswerValue(fieldName);  }
            set { FieldAnswers[fieldName] = value; }
        }

        public Dictionary<string, object> FieldAnswers { get; set; }

        public bool IsFieldAnswered(string fieldName)
        {
            var value = GetFieldAnswerValue(fieldName);
            if (value == null)
                return false;

            if (value is string valueString)
                return !String.IsNullOrEmpty(valueString);

            return true;
        }

        public string GetFieldAnswer(string fieldName)
        {
            return GetFieldAnswer<string>(fieldName);
        }

        public T GetFieldAnswer<T>(string fieldName)
        {
            var answer = GetFieldAnswerValue(fieldName);

            if (answer == null)
                return default(T);

            // Special handling for JArray caused by serialization of the data
            if (answer is JArray jarrayAnswer)
                return (jarrayAnswer).ToObject<T>();

            if (answer is T)
                return (T)answer;

            try
            {
                return ScriptHelpers.ConvertToType<T>(answer);
            }
            catch (JsonReaderException)
            {

            }

            return (T)Convert.ChangeType(answer, typeof(T));
        }

        private object GetFieldAnswerValue(string fieldName)
        {
            object answer = null;

            if (FieldAnswers.ContainsKey(fieldName))
                answer = FieldAnswers[fieldName];

            return answer;
        }
    }
}