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
    [Serializable]
    public class DeviceMatchResult
    {
        /// <summary>
        /// Unique identifier of the device
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Probability make being an accurate match
        /// </summary>
        public double Ratio { get; set; }
        /// <summary>
        /// Detected device make 
        /// </summary>
        public string Make { get; set; }
        /// <summary>
        /// Detected device model
        /// </summary>
        public string Model { get; set; }
        /// <summary>
        /// Detected device display name 
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// Flag to indicate if this is from the list of uncommon devices.  
        /// Uncommon devices should have a higher confidence level for matching.
        /// </summary>
        public bool IsUncommon { get; set; }
    }
}