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

namespace ChatWeb.Models
{
    [Serializable]
    public class ClassificationResults
    {
        public ClassificationResults(double threshold)
        {
            this.Threshold = threshold;
        }

        public double Threshold { get; private set; }

        public ClassificationResponse[] CommonChatResults { get; set; }

        public ClassificationResponse[] ClassifierResults { get; set; }

        public ClassificationResponse[] TopResults
        {
            get
            {
                if (CommonChatResults != null)
                {
                    // Filter low probability scores
                    var commonResults = (from r in CommonChatResults
                                         where (r.Probability > ChatConfiguration.MinimumLuisCommonChatConfidenceRatio)
                                         select r).ToArray();
                    if ((commonResults?.Length > 0) && (commonResults[0].Intent != "commonchat-None"))
                        return commonResults;
                }

                if (ClassifierResults != null)
                {
                    return (from i in ClassifierResults
                            where i.Probability > Threshold
                                          select i).Take(2).ToArray();
                }

                return new ClassificationResponse[0];
            }
        }

        public List<ClassificationResponse> AllResults
        {
            get
            {
                var result = new List<ClassificationResponse>();
                if (CommonChatResults != null)
                    result.AddRange(CommonChatResults);
                if (ClassifierResults != null)
                    result.AddRange(ClassifierResults);

                return result;
            }
        }

        public ClassificationResponse GetBestResult()
        {
            if (TopResults?.Length > 0)
                return TopResults[0];

            return null;
        }

        public bool IsSuccessful
        {
            get
            {
                return TopResults?.Select(x => x.Intent != "commonchat-None").Count() > 0;
            }
        }
    }

    /// <summary>
    /// Result of the classification of a test message
    /// </summary>
    [Serializable]
    public class ClassificationResponse
    {
        /// <summary>
        /// Detected category of the message
        /// </summary>
        public string Intent { get; set; }
        /// <summary>
        /// Processed result of the message
        /// </summary>
        public object Result { get; set; }
        /// <summary>
        /// Probability of the intent being accurate
        /// </summary>
        public double Probability { get; set; }

        /// <summary>
        /// Matching Soluto Reason
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Matching Soluto Subreason
        /// </summary>
        public string Subreason { get; set; }

        /// <summary>
        /// Source of classification
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Data specific to the source of the request.  IE:  classifier name in azureml
        /// </summary>
        public string SourceData { get; set; }

        /// <summary>
        /// Full response from classifier
        /// </summary>
        public string RawResponse { get; set; }
    }
}