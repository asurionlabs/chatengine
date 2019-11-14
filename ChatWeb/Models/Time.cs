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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ChatWeb.Models
{
    [Serializable]
    public class Time
    {
        private DateTime dateTime;

        public Time() : this(new TimeSpan())
        {
        }

        public Time(TimeSpan timeSpan) : this (DateTime.Today.Add(timeSpan))
        {
        }

        public Time(DateTime dateTime)
        {
            this.dateTime = dateTime;
        }

        public TimeSpan TimeSpan
        {
            get
            {
                return dateTime.TimeOfDay;
            }
            set
            {
                dateTime = DateTime.Today.Add(value);
            }
        }
        public string Format24Hour
        {
            get
            {
                return dateTime.ToString("HH:mm");
            }
        }

        public string Format12Hour
        {
            get
            {
                return dateTime.ToString("h:mm tt");
            }
        }

        public override bool Equals(object obj)
        {
            return dateTime.TimeOfDay.Equals(((Time)obj).TimeSpan);
        }

        public override int GetHashCode()
        {
            return dateTime.TimeOfDay.GetHashCode();
        }
    }
}