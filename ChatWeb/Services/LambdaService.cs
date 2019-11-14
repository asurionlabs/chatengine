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

using Amazon.Lambda;
using Amazon.Lambda.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace ChatWeb.Services
{
    public class LambdaService : IDisposable
    {
        AmazonLambdaClient client = new AmazonLambdaClient();
        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (client != null)
                    {
                        client.Dispose();
                        client = null;
                    }
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion

        public string Invoke(string function, string payload)
        {
            InvokeRequest request = new InvokeRequest()
            {
                FunctionName = function,
                InvocationType = InvocationType.RequestResponse,
                Payload = payload
            };

            try
            {
                var response = client.Invoke(request);
                if (response?.StatusCode == 200)
                {
                    using (var sr = new StreamReader(response.Payload))
                    {
                        /*JsonReader reader = new JsonTextReader(sr);

                        var serilizer = new JsonSerializer();
                        var op = serilizer.Deserialize(reader);
                        */
                        return sr.ReadToEnd();
                    }
                }
            }
            catch(Exception ex)
            {
                logger.ErrorFormat("Network: Failed to call Lambda {0}. Error: {1}", function, ex.Message);
            }

            return null;
        }

    }
}