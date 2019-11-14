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
using System.Text;
using System.Threading.Tasks;

namespace ChatWeb.Models
{
    [Serializable]
    public class ChatStepId
    {
        public ChatStepId()
        {
        }

        public ChatStepId(string flowName, string stepId)
        {
            this.FlowName = flowName;
            this.StepId = stepId;
        }

        public ChatStepId(ChatFlowStep step)
        {
            if (step != null)
            {
                this.FlowName = step.Flow;
                this.StepId = step.Id;
            }
        }

        public string FlowName { get; set; }
        public string StepId { get; set; }

        public string MessageId
        {
            get
            {
                return $"{FlowName}-{StepId}";
            }
        }
    }
}
