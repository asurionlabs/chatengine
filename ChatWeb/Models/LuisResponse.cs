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
using System.Linq;

namespace ChatWeb.Models
{
    [Serializable]
    public class LuisResponse
    {
        public string Query { get; set; }
        public LuisIntent TopScoringIntent { get; set; }
        public LuisIntent[] Intents { get; set; }
        public LuisEntity[] Entities { get; set; }

        public string GetTopIntent(double minScore)
        {
            string intent = null;

            if (TopScoringIntent?.Score > minScore)
                intent = TopScoringIntent.Intent;

            if (String.IsNullOrEmpty(intent) && (Intents?.Length > 0))
            {
                // Try date resolution first
                intent = (from e in Intents
                          orderby e.Score descending
                          where e.Score > minScore
                          select e.Intent).FirstOrDefault();
            }

            return intent;
        }

        public string[] GetIntents(double minScore)
        {
            string[] intent = null;

            if (Intents?.Length > 0)
            {
                intent = (from e in Intents
                          orderby e.Score descending
                          where e.Score > minScore
                          select e.Intent).ToArray();
            }

            return intent;
        }
    }

    [Serializable]
    public class LuisIntent
    {
        public string Intent { get; set; }
        public float Score { get; set; }
    }


    [Serializable]
    public class LuisEntity
    {
        public string Entity { get; set; }
        public string Type { get; set; }
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        public LuisEntityResolution Resolution { get; set; }
    }

    [Serializable]
    public class LuisEntityResolution
    {
        // From datetime (v1)
        public string Date { get; set; }
        public string Time { get; set; }

        // Used by datetimeV2
        public LuisEntityValue[] Values { get; set; }
    }

    [Serializable]
    public class LuisEntityValue
    {
        public string Timex { get; set; }
        public string Type { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
        public string Value { get; set; }
        public string Mod { get; set; }

    }

}

