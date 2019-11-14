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
    /// <summary>
    /// TextAndDateTime Class used to store  text and date time object 
    /// from Microsoft Recognizer recognition result.
    /// 
    /// Taken from Microsoft Bot Builder SDK source.
    /// LocaleConverter.cs
    /// https://github.com/Microsoft/botbuilder-dotnet/tree/master/libraries/Microsoft.Bot.Builder.Ai.Translation
    /// </summary> 
    [Serializable]
    public class DateTimeParseResult
    {
        public string Text { get; set; }
        public DateTime? DateTime { get; set; }
        public Time Time { get; set; }
        public string Type { get; set; }
        public bool Range { get; set; }
        public DateTime? EndDateTime { get; set; }
        public Time EndTime { get; set; }
        public string Modifier { get; set; }
    }

    public class DateTimeParseResultEqualityComparer : EqualityComparer<DateTimeParseResult>
    {
        public override bool Equals(DateTimeParseResult a, DateTimeParseResult b)
        {
            return ((a.Text == b.Text) &&
                (a.Modifier == b.Modifier) &&
                (a.DateTime == b.DateTime) &&
                (a.Time == b.Time) &&
                (a.EndDateTime == b.EndDateTime) &&
                (a.EndTime == b.EndTime)
                );
        }

        public override int GetHashCode(DateTimeParseResult value)
        {
            unchecked
            {
                int hash = 17;
                hash = 31 * hash + (value.Text == null ? 0 : value.Text.GetHashCode());
                hash = 31 * hash + (value.Modifier == null ? 0 : value.Modifier.GetHashCode());
                hash = 31 * hash + (value.DateTime == null ? 0 : value.DateTime.GetHashCode());
                hash = 31 * hash + (value.Time == null ? 0 : value.Time.GetHashCode());
                hash = 31 * hash + (value.EndDateTime == null ? 0 : value.EndDateTime.GetHashCode());
                hash = 31 * hash + (value.EndTime == null ? 0 : value.EndTime.GetHashCode());

                return hash;
            }
        }
    }
}

