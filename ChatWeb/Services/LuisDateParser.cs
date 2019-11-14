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
using System.Text.RegularExpressions;
using System.Web;

namespace ChatWeb.Services
{
    public class LuisDateParser
    {
        const int DaysInWeek = 7;
        static readonly DateTimeParseResultEqualityComparer comparer = new DateTimeParseResultEqualityComparer();
        static readonly Time StartOfDay = new Time(new TimeSpan(0, 0, 0));
        static readonly Time EndOfDay = new Time(new TimeSpan(23, 59, 59));

        readonly DateTime BaselineNow;
        readonly int UserTimeZoneOffset;

        public LuisDateParser(int userTimeZoneOffset)
        {
            BaselineNow = DateTime.UtcNow;
            this.UserTimeZoneOffset = userTimeZoneOffset;
        }

        public LuisDateParser(DateTime now, int userTimeZoneOffset)
        {
            BaselineNow = now.ToUniversalTime();
            this.UserTimeZoneOffset = userTimeZoneOffset;
        }

        DateTimeParseResult[] ExtractDates(IEnumerable<LuisEntity> dateEntities)
        {
            var foundDates = new List<DateTimeParseResult>();

            foreach (var entity in dateEntities)
            {
                string type = entity.Type.Replace("builtin.datetimeV2.", "");

                if (entity.Resolution != null)
                {
                    foreach (var resolutionValue in entity.Resolution.Values)
                    {
                        var date = ExtractDate(entity, type, resolutionValue);
                        if (date != null)
                            foundDates.Add(date);
                    }
                }
            }

            //return foundDates.Distinct(comparer).ToArray();
            return foundDates.ToArray();
        }

        private DateTimeParseResult ExtractDate(LuisEntity entity, string type, LuisEntityValue resolutionValue)
        {
            if (resolutionValue.Value == "not resolved")
                return null;

            DateTimeParseResult result = new DateTimeParseResult
            {
                Text = entity.Entity,
                Type = GetMomentType(type),
                Modifier = resolutionValue.Mod
            };

            if (type.Contains("range"))
            {
                result.Range = true;

                if (result.Type.Contains("time"))
                {
                    result.Time = ParseTime(resolutionValue.Start, result.Type);
                    result.EndTime = ParseTime(resolutionValue.End, result.Type);
                }

                if (result.Type.Contains("date"))
                {
                    result.DateTime = ParseDate(resolutionValue.Start, result.Type);
                    result.EndDateTime = ParseDate(resolutionValue.End, result.Type);

                    // Special case when Luis gives daterange, but does not resolve.
                    // Example:  Summer
                    if ((result.DateTime == null) && (result.EndDateTime == null))
                        return null;

                    // Special case for date range.  should advance to end of day
                    if (String.IsNullOrEmpty(result.Modifier) && result.Type == "date")
                        result.EndDateTime = result.EndDateTime.Value.Add(EndOfDay.TimeSpan);

                    // Special case for "after" on date ranges.  should advance to next day.
                    if (result.Modifier == "after" && result.Type == "date")
                        result.DateTime = result.DateTime.Value.AddDays(1);
                }
            }
            else
            {
                if (result.Type.Contains("time"))
                    result.Time = ParseTime(resolutionValue.Value, result.Type);

                if (result.Type.Contains("date"))
                    result.DateTime = ParseDate(resolutionValue.Value, result.Type);
            }

            return result;
        }

        Time ParseTime(string value, string type)
        {
            var dateTime = ParseDate(value, type);
            if (dateTime == null)
                return null;

            return new Time(dateTime.Value);
        }

        DateTime? ParseDate(string value, string type)
        {
            // Set Kind to Utc
            if (DateTime.TryParse(value, out DateTime result))
                return new DateTime(result.Year, result.Month, result.Day, result.Hour, result.Minute, result.Second, DateTimeKind.Utc);

            return null;
        }

        private static string GetMomentType(string type)
        {
            string momentType;
            if (type.Contains("date") && type.Contains("time"))
                momentType = "datetime";
            else if (type.Contains("date"))
                momentType = "date";
            else
                momentType = "time";
            return momentType;
        }

        public DateTimeParseResult[] ParseLuisTimes(LuisEntity[] luisEntities)
        {
            var times = from date in ExtractDates(luisEntities)
                        where (date.Type == "time" || date.Type == "datetime")
                        select date;

            return times.ToArray();
        }

        public DateTimeParseResult[] ParseLuisDates(LuisEntity[] luisEntities, bool assumeFuture)
        {
            var dates = ExtractDates(luisEntities).ToList();
            var filteredDates = new List<DateTimeParseResult>();

            foreach (var date in dates.ToArray())
            {
                if (date.Type == "time")
                {
                    SetDefaultTimeForTimes(date);
                    filteredDates.Add(date);
                }
                else
                {
                    SetDefaultTimesForDates(date);

                    if (IsInExpectedRange(assumeFuture, date))
                        filteredDates.Add(date);
                }
            }

            // Return filtered dates if there are any.
            // This will handle cases like 'monday' that return 2 dates, but we filter to the right one.
            if (filteredDates.Count > 0)
                return filteredDates.ToArray();

            // Otherwise return all dates.
            return dates.ToArray();
        }

        private bool IsInExpectedRange(bool shouldBeFuture, DateTimeParseResult date)
        {
            if (shouldBeFuture)
            {
                if (((date.EndDateTime != null) && (date.EndDateTime <= BaselineNow)) ||
                   ((date.EndDateTime == null) && (date.DateTime < BaselineNow.Date)))
                    return false;
            }
            else
            {
                if (((date.DateTime != null) && date.DateTime > BaselineNow.Date) ||
                    ((date.DateTime == null) && (date.EndDateTime >= BaselineNow)))
                    return false;
            }

            return true;
        }

        private static void SetDefaultTimeForTimes(DateTimeParseResult date)
        {
            if (date.Time == null)
                date.Time = date.Range ? StartOfDay : date.EndTime;

            if (date.EndTime == null)
                date.EndTime = date.Range ? EndOfDay : date.Time;
        }

        void SetDefaultTimesForDates(DateTimeParseResult date)
        {
            if (date.DateTime == null)
            {
                if (!date.Range || date.Type.Contains("time"))
                    date.DateTime = date.EndDateTime.Value.Date;
            }

            if (date.EndDateTime == null)
            {
                if (!date.Range && date.Type.Contains("time"))
                    date.EndDateTime = date.DateTime;
                else if (!date.Range || date.Type.Contains("time"))
                    date.EndDateTime = date.DateTime.Value.Date.Add(EndOfDay.TimeSpan);
            }

            if ((date.Time == null) && date.DateTime.HasValue)
                date.Time = new Time(date.DateTime.Value.TimeOfDay);

            if ((date.EndTime == null) && date.EndDateTime.HasValue)
                date.EndTime = new Time(date.EndDateTime.Value.TimeOfDay);
        }


        private DateTime CreateLocalizedDate(int yearNum, int monthNum, int dayNum)
        {
            // Luis gets the user's timezone and localizes the date.  But we need to attach the users current hour to the date to assist with conversions
            // to other time zones.  Time zones can affect the actual date.
            // 1. Create date object including user's current time.
            // 2. Adjust to UTC to form proper DateTime UTC object.

            try
            {
                var usersCurrentHour = CalculateUsersCurrentHour(BaselineNow.Hour, UserTimeZoneOffset);
                var date = new DateTime(yearNum, monthNum, dayNum, usersCurrentHour, BaselineNow.Minute, BaselineNow.Second, DateTimeKind.Utc);

                // Convert back to true UTC
                return date.AddHours(-UserTimeZoneOffset);
            }
            catch (ArgumentOutOfRangeException)
            {
                // This happens if the user specified a valid date form, but not valid dates.  Ex:  2/31/2018
                return DateTime.MinValue;
            }
        }


        //WORKS for SErver in UTC.  Need to fix for server in PST
        static int CalculateUsersCurrentHour(int utcHour, int offset)
        {
            var target = utcHour + offset;
            if (target >= 24)
                target = target - 24;
            if (target < 0)
                target = target + 24;

            return target;
        }

    }
}