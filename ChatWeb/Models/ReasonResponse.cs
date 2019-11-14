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
    /// <summary>
    /// Response object
    /// </summary>
    public class ReasonResponse
    {
        /// <summary>
        /// List of Reasons
        /// </summary>
        public ReasonProbability[] Reasons { get; set; }
    }

    /// <summary>
    /// Reason Probability
    /// </summary>
    public class ReasonProbability
    {
        /// <summary>
        /// Calculated reason for the user's given input
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Calculated subreason for the user's given input
        /// </summary>
        public string SubReason { get; set; }

        /// <summary>
        /// Probability the reason is correct
        /// </summary>
        public double Probability { get; set; }
    }
}
