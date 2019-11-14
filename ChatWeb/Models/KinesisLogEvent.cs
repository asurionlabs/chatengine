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

namespace ChatWeb.Models
{
    public class KinesisLogEvent
    {
        /// <summary>
        /// REQUIRED (unique user session id)
        /// </summary>
        public string Session_id { get; set; }

        /// <summary>
        /// REQUIRED 'Can contain PII information and will be encrypted
        /// </summary>
        public string Log { get; set; }

        /// <summary>
        /// OPTIONAL(logging application id e.g. if you are calling from flow engine 'flow_engine:flow_name:step_id')
        /// </summary>
        public string Logger_info { get; set; }
        
        /// <summary>
        /// OPTIONAL(additional info packed into string)
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// OPTIONAL(application)
        /// </summary>
        public string Application_info { get; set; }

        /// <summary>
        /// Flag to indicate this messaged should be indexed for searching
        /// </summary>
        public bool To_index { get; set; }

        /// <summary>
        /// Type of index to store the data
        /// </summary>
        public string Index_type { get; set; }

        /// <summary>
        /// Name of index to store the data
        /// </summary>
        public string Index_name { get; set; }

        /// <summary>
        /// Data to be indexed
        /// </summary>
        public object Index_content { get; set; }
    }

    public class KinesisLogMessage
    {
        public string User { get; set; }
        public string Ava { get; set; }
    }

    public class KinesisTranscriptData
    {
        public long ProcessTime { get; set; }
        public string DetectedIntent { get; set; }
        public double Probability { get; set; }
        public ChatStepId[] StepsTaken { get; set; }
        public bool AutoPause { get; set; }
        public bool AutoPopulate { get; set; }
        public string Channel { get; set; }
    }

    public class KinesisParseEventContent
    {
        public string Session_id { get; set; }
        public string Chat_id { get; set; }
        public string Interaction_id { get; set; }
        public string Session_date { get; set; }
        public string Variable_name { get; set; }
        public ChatStepId FlowStep { get; set; }
        public string Bot_text { get; set; }
        public string Chat_text { get; set; }
        public string Chat_corrected_text { get; set; }
        public bool Success { get; set; }
        public bool SearchMode { get; set; }
        public string Source { get; set; }
        public string Result { get; set; }
        public object ResultData { get; set; }
        public double ResultProbability { get; set; }
        public KinesisClassification[] Classifications {get;set;}
        public object Parse { get; set; }
        public Dictionary<string, object> Properties { get; set; }
    }

    public class KinesisClassification
    {
        public string Model_source { get; set; }
        public string Model_prediction { get; set; }
        public double Model_score { get; set; }
        public string Model_rawResponse { get; set; }
    }
}