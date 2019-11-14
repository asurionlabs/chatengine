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

using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatWeb.Helpers
{
    /// <summary>
    /// AWSSDK DynamoDB can convert Enums from string's when loading from the database,
    /// but if the string is not a valid value in the enum, it throws an exception.
    /// 
    /// This conversion returns the enum's default value (0) in that case.
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    public class UnknownEnumConverter<TEnum> : IPropertyConverter where TEnum : struct 
    {
        public DynamoDBEntry ToEntry(object value)
        {
            string valueAsString = value.ToString();

            DynamoDBEntry entry = new Primitive(valueAsString);

            return entry;
        }

        public object FromEntry(DynamoDBEntry entry)
        {
            string valueAsString = entry.AsString();
            if (String.IsNullOrEmpty(valueAsString))
                return default(TEnum);

            if (Enum.TryParse<TEnum>(valueAsString, true, out TEnum valueAsEnum) && Enum.IsDefined(typeof(TEnum), valueAsEnum))
                return valueAsEnum;

            return default(TEnum);
        }
    }
}
