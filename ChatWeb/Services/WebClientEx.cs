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
using System.Net;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using ChatWeb.Helpers;

namespace ChatWeb.Services
{
    public class WebClientEx : WebClient
    {
        /// <summary>
        /// Time in milliseconds
        /// </summary>
        public int Timeout { get; set; }

        public WebClientEx() : this(60000) { }

        public WebClientEx(int timeout)
        {
            this.Timeout = timeout;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            if (request != null)
                request.Timeout = this.Timeout;

            if (AwsUtilityMethods.IsRunningOnAWS)
            {
                AWSXRayRecorder.Instance.BeginSubsegment(request.RequestUri.Host);
                AWSXRayRecorder.Instance.SetNamespace("remote");
                Dictionary<string, object> requestInformation = new Dictionary<string, object>();
                requestInformation["url"] = request.RequestUri.AbsoluteUri;
                requestInformation["method"] = request.Method;
                AWSXRayRecorder.Instance.AddHttpInformation("request", requestInformation);

                if (TraceHeader.TryParse(TraceContext.GetEntity(), out TraceHeader header))
                    request.Headers.Add("X-Amzn-Trace-Id", header.ToString());
            }

            return request;
        }

        protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
        {
            try
            {
                HttpWebResponse response = (HttpWebResponse)base.GetWebResponse(request, result);
                if (AwsUtilityMethods.IsRunningOnAWS)
                    TraceResponse(response);
                return response;
            }
            catch (Exception e)
            {
                if (AwsUtilityMethods.IsRunningOnAWS)
                    AWSXRayRecorder.Instance.AddException(e);
                throw;
            }
            finally
            {
                if (AwsUtilityMethods.IsRunningOnAWS)
                    AWSXRayRecorder.Instance.EndSubsegment();
            }
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            try
            {
                HttpWebResponse response = (HttpWebResponse)base.GetWebResponse(request);
                if (AwsUtilityMethods.IsRunningOnAWS)
                    TraceResponse(response);
                return response;
            }
            catch (Exception e)
            {
                if (AwsUtilityMethods.IsRunningOnAWS)
                    AWSXRayRecorder.Instance.AddException(e);
                throw;
            }
            finally
            {
                if (AwsUtilityMethods.IsRunningOnAWS)
                    AWSXRayRecorder.Instance.EndSubsegment();
            }
        }

        private void TraceResponse(HttpWebResponse response)
        {
            Dictionary<string, object> responseInformation = new Dictionary<string, object>();
            int statusCode = (int)response.StatusCode;
            responseInformation["status"] = statusCode;
            if (statusCode >= 400 && statusCode <= 499)
            {
                AWSXRayRecorder.Instance.MarkError();
                if (statusCode == 429)
                {
                    AWSXRayRecorder.Instance.MarkThrottle();
                }
            }
            else if (statusCode >= 500 && statusCode <= 599)
            {
                AWSXRayRecorder.Instance.MarkFault();
            }
            responseInformation["content_length"] = response.ContentLength;
            AWSXRayRecorder.Instance.AddHttpInformation("response", responseInformation);
        }
    }
}
