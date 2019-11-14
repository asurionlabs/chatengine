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

using Amazon.XRay.Recorder.Handlers.System.Net;
using ChatWeb.Helpers;
using ChatWeb.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace ChatWeb.Services
{
    public class RestApiService
    {
        const int DefaultServiceRetries = 3;

        readonly int retries;
        readonly int warnSendTime;
        readonly string baseUrl;
        readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        readonly Dictionary<string, string> headers;
        readonly string apiKey;

#if XRAY2
        static readonly HttpClient client = AwsUtilityMethods.IsRunningOnAWS ? new HttpClient(new HttpClientXRayTracingHandler(new HttpClientHandler())) : new HttpClient();
#else
        static readonly HttpClient client = HttpClientFactory.Create(new[] { new XRayTracingMessageHandler() });
#endif

        static readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        };

        public RestApiService(UrlConfig urlConfig, Dictionary<string, string> headers = null, int retries = DefaultServiceRetries, int warnSendTime = ChatConfiguration.DefaultRestAPIWarnSendTime)
            : this(urlConfig.Url, urlConfig.Key, headers, retries, warnSendTime)
        {
        }

        /// <summary>
        /// Wraps calls to REST Api's by including retry logic and wrapping JSON.
        /// </summary>
        /// <param name="baseUrl">Base Url of the API</param>
        /// <param name="apiKey">API key. sets x-api-key header. (optional)</param>
        /// <param name="headers">Optional headers to put in all requests</param>
        /// <param name="retries">Number of retries.</param>
        /// <param name="warnSendTime">Logs a warning message if the API takes longer than the warning time to return.  -1 to disable the warning.</param>
        public RestApiService(string baseUrl, string apiKey, Dictionary<string, string> headers = null, int retries = DefaultServiceRetries, int warnSendTime = ChatConfiguration.DefaultRestAPIWarnSendTime)
        {
            this.apiKey = apiKey;
            this.retries = retries;
            this.warnSendTime = warnSendTime;
            this.baseUrl = baseUrl;
            this.headers = headers;
        }

        public string BaseUrl
        {
            get => baseUrl;
        }

        public async Task<T> CallRestApi<T>(string endpoint, object value, int timeout = 0)
        {
            var requestText = JsonConvert.SerializeObject(value, serializerSettings);

            return await CallRestApi<T>(endpoint, requestText, "application/json", timeout);
        }

        public async Task<T> CallRestApi<T>(string endpoint, string value, string contentType, int timeout = 0)
        {
            var response = await CallRestApi(endpoint, value, contentType, timeout);
            if (response == null)
                return default(T);

            try
            {
                return JsonConvert.DeserializeObject<T>(response, serializerSettings);
            }
            catch (JsonReaderException ex)
            {
                logger.ErrorFormat("Network: Error deserializing rest api response. {0}.  Error: {1}", endpoint, ex);
            }

            return default(T);
        }

        public async Task<string> CallRestApi(string endpoint, object value, int timeout = 0)
        {
            var requestText = JsonConvert.SerializeObject(value, serializerSettings);

            return await CallRestApi(endpoint, requestText, "application/json", timeout);
        }

        public async Task<string> CallRestApi(string endpoint, string text, string contentType, int timeout = 0)
        {
            int tries = 1;
            string url = baseUrl + endpoint;

            try
            {
                do
                {
                    (HttpStatusCode statusCode, string result) = await MakeApiCall(url, text, contentType, timeout);
                    if (result != null)
                        return result;
                    else
                        logger.ErrorFormat("Network: REST API call failed. Attempt {0}/{1}. Status Code: {2}. Url: {3}", tries, retries, statusCode, url);


                } while (tries++ < retries);
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                if (ex is TaskCanceledException taskCanceledException)
                    message = "Timed out waiting for response.";

                logger.ErrorFormat("Network: Failed to call rest api {0}. Error: {1}", url, message);
            }

            return null;
        }

        private async Task<(HttpStatusCode, string)> MakeApiCall(string url, string text, string contentType, int timeout)
        {
            string result = null;
            HttpStatusCode httpStatusCode = HttpStatusCode.OK;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            using (HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, url))
            {
                httpRequest.Content = new StringContent(text, Encoding.UTF8, contentType);

                SetHeaders(httpRequest);

                try
                {
                    var cs = new CancellationTokenSource();
                    if (timeout > 0)
                        cs.CancelAfter(timeout);

                    using (HttpResponseMessage response = await client.SendAsync(httpRequest, cs.Token).ConfigureAwait(false))
                    {
                        httpStatusCode = response.StatusCode;
                        if (response.IsSuccessStatusCode)
                            result = await response.Content.ReadAsStringAsync();
                    }
                }
                catch (InvalidOperationException ex)
                {
                    logger.ErrorFormat("Network: Failed to call REST API {0}. Error: {1}", url, ex.Message);
                }
            }

            sw.Stop();
            var elapsedTime = sw.Elapsed.TotalSeconds;
            if ((warnSendTime >= 0) && (elapsedTime > warnSendTime))
                logger.WarnFormat("Network: RestApiService.Send() took {0}. Status Code: {1}. Url: {2}", elapsedTime, httpStatusCode, url);

            return (httpStatusCode, result);
        }

        private void SetHeaders(HttpRequestMessage httpRequest)
        {
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!String.IsNullOrEmpty(apiKey))
            {
                httpRequest.Headers.Add("x-api-key", apiKey);
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    httpRequest.Headers.Add(header.Key, header.Value);
                }
            }
        }
    }
}