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

namespace ChatWeb.Helpers
{
    public sealed class UtilityMethods
    {
        private UtilityMethods() { }

        public static bool IsValueType(object value)
        {
            return value != null && value.GetType().IsValueType;
        }

        public static bool IsNullOrEmpty<T>(T[] array)
        {
            return array == null || array.Length == 0;
        }

        /// <summary>
        /// Evaluates an object to true/false using the same logic as Javascript.
        /// https://www.w3schools.com/JS/js_booleans.asp
        /// </summary>
        /// <param name="value">object to evaluate</param>
        /// <returns>true or false following javascript true/false evaluation rules.</returns>
        public static bool ParseJavascriptBoolean(object value)
        {
            if (value == null)
                return false;

            if (value is bool bln)
                return bln;

            if (value is string str)
                return str.Length != 0;

            if (value is int num)
                return num != 0;

            if (value is Microsoft.ClearScript.Undefined undefined)
                return false;

            return true;
        }
    }
}