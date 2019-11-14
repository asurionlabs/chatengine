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

using Dynamitey;
using Microsoft.ClearScript;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ChatWeb.Helpers
{
    public static class ScriptHelpers
    {
        static DateTime UnixStartDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static object ToScriptValue(this object itemVal)
        {
            return ToScriptValue(itemVal, null);
        }

        public static object ToScriptValue(this object itemVal, ScriptEngine context)
        {
            if (context == null)
                context = ScriptEngine.Current;

            if (context == null)
                throw new ArgumentNullException(nameof(context), "Must specify ScriptEngine if not being called from Javascript engine");

            if (itemVal == null)
                return itemVal;

            if (itemVal is string)
                return itemVal;

            if (itemVal is JArray)
                return ToScriptArray((JArray)itemVal, context);

            if (itemVal is JValue)
            {
                object o = ((JValue)itemVal).Value;
                if (o is DateTime)
                    return ToJSDate((DateTime)o, context);
                return o;
            }

            if (itemVal is DateTime)
                return ToJSDate((DateTime)itemVal, context);

            if (itemVal is object[])
                return ToScriptArray((object[])itemVal, context);

            if (itemVal is List<object>)
                return ToScriptArray<object>((List<object>)itemVal, context);

            if (itemVal is JObject)
            {
                var dict = (JObject)itemVal;
                return ToScriptObject<JToken>(dict, context);
            }

            if (itemVal is Dictionary<string, object>)
            {
                var dict = (IDictionary<string, object>)itemVal;
                return ToScriptObject<object>(dict, context);
            }

            if (!UtilityMethods.IsValueType(itemVal))
            {
                // Trick to convert to JObject
                var t = JsonConvert.SerializeObject(itemVal);
                return ToScriptValue(JsonConvert.DeserializeObject<dynamic>(t), context);
            }

            return itemVal;
        }

        public static DateTime? FromJSDate(dynamic jsDate)
        {
            // Call valueOf() method on JS object directly
            var value = jsDate.valueOf();
            if (value is long)
                return UnixStartDate.AddMilliseconds((long)value);
            else if (value is double)
                return UnixStartDate.AddMilliseconds((double)value);

            return null;
        }

        static object ToJSDate(DateTime date, ScriptEngine context)
        {
            // Make sure incoming date has Kind set for proper conversion
            if (date.Kind == DateTimeKind.Unspecified)
                date = new DateTime(date.Ticks, DateTimeKind.Utc);

            double ms = (date - UnixStartDate).TotalMilliseconds;

            return context.Evaluate($"new Date({ms});");
        }

        static object ToScriptObject<T>(IDictionary<string, T> dictionary, ScriptEngine context)
        {
            dynamic dict = context.Evaluate("new Object()");
            foreach (var item in dictionary)
            {
                Dynamic.InvokeSet(dict, item.Key, ToScriptValue(item.Value, context));
            }

            return dict;
        }

        static object ToScriptArray<T>(IEnumerable<T> array, ScriptEngine context)
        {
            dynamic scriptArray = context.Evaluate("[]");
            foreach (var element in array)
            {
                scriptArray.push(ToScriptValue(element, context));
            }
            return scriptArray;
        }

        public static object FromScriptValue(this object value)
        {
            return ConvertScriptValue(value);
        }

        private static object ConvertScriptValue(object itemVal)
        {
            if ((itemVal == null) || (itemVal is Microsoft.ClearScript.Undefined))
                return null;

            if (itemVal is JArray jArray)
            {
                var arr = jArray.ToObject<object[]>();
                return ConvertScriptValues(arr);
            }

            if (itemVal is JValue jValue)
                return jValue.Value;

            if (itemVal is object[] objArray)
                return ConvertScriptValues(objArray);

            if (itemVal is List<object> listObjects)
                return ConvertScriptValues(listObjects);

            if (itemVal is IDictionary<string, object> itemDict)
            {
                var newDictionary = new Dictionary<string, object>();

                foreach (var item in itemDict)
                {
                    newDictionary.Add(item.Key, ConvertScriptValue(item.Value));
                }
                return newDictionary;
            }

            if ((itemVal is string) || (itemVal is int) || (itemVal is double))
                return itemVal;

            if (itemVal is JObject jObject)
            {
                if (!jObject.HasValues)
                    return new object();

                IDictionary<string, JToken> jObjectDict = jObject as IDictionary<string, JToken>;
                var newDictionary = new Dictionary<string, object>();

                foreach (var item in jObjectDict)
                {
                    newDictionary.Add(item.Key, ConvertScriptValue(item.Value));
                }
                return newDictionary;
            }

            var t = itemVal.GetType();

            if ((t.FullName == "Microsoft.ClearScript.V8.V8ScriptItem") ||
                (t.FullName == "Microsoft.ClearScript.Windows.WindowsScriptItem"))
                return ConvertNativeScriptValue(itemVal);


            return itemVal;
        }

        private static object ConvertNativeScriptValue(object itemVal)
        {
            DynamicObject item = (DynamicObject)itemVal;
            var memberNames = item.GetDynamicMemberNames().ToArray();

            dynamic d = itemVal;
            if (memberNames.Contains("length") || memberNames.Contains("0"))
            {
                var length = d.length;

                if (length.GetType() != typeof(Microsoft.ClearScript.Undefined))
                {
                    var arr = new object[length];
                    for (int i = 0; i < length; i++)
                    {
                        arr[i] = ConvertScriptValue(d[i]);
                    }

                    return arr;
                }
            }

            try
            {
                // Try to convert as JS Date?
                var date = FromJSDate(itemVal);
                if (date != null)
                    return date;
            }
            catch (ScriptEngineException)
            {
                // Probably not a JS Date
            }
            catch (RuntimeBinderException)
            {
            }

            if (memberNames.Length > 0)
            {
                var dictionary = new Dictionary<string, object>();

                foreach (string name in memberNames)
                {
                    var memberValue = Dynamic.InvokeGet(itemVal, name);
                    dictionary[name] = ConvertScriptValue(memberValue);
                }

                return dictionary;
            }

            // Last try
            if (Dynamic.InvokeMember(itemVal, "toString", null) == "[object Object]")
                return new object();

            return new object[0];
        }

        private static object[] ConvertScriptValues(IEnumerable<object> itemVal)
        {
            var newList = new List<object>();

            foreach (var obj in itemVal)
            {
                newList.Add(ConvertScriptValue(obj));
            }

            return newList.ToArray();
        }

        public static dynamic ToDynamic(this IDictionary<string, object> dictionary, ScriptEngine context)
        {
            ExpandoObject eo = new ExpandoObject();
            var dict = (IDictionary<string, object>)eo;

            foreach (var item in dictionary)
            {
                dict.Add(item.Key, ToScriptValue(item.Value, context));
            }

            return eo;
        }

        /// <summary>
        /// Convert from unknown object type to known object type
        /// </summary>
        /// <typeparam name="T">Type of object to get</typeparam>
        /// <param name="value">Object to convert</param>
        /// <returns>Converted object</returns>
        public static T ConvertToType<T>(object value)
        {
            var t = JsonConvert.SerializeObject(value);
            return JsonConvert.DeserializeObject<T>(t);
        }

    }
}
