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

namespace ChatWeb.Models
{
    // Replaces built in library Stack type with List based object.
    // This is done because JSON.NET does not deserialize Stack object in the correct order
    // and messing with custom converters is error prone.
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.Fields)]
    [Serializable]
    public class SimpleStack<T>
    {
        readonly List<T> list = new List<T>();

        public void Clear()
        {
            list.Clear();
        }

        public int Count {  get { return list.Count;  } }

        public T Pop()
        {
            if (list.Count == 0)
                throw new InvalidOperationException("Stack is empty");

            var item = list[list.Count - 1];
            list.Remove(item);

            return item;
        }

        public void Push(T item)
        {
            list.Add(item);
        }
    }
}