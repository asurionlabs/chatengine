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

//#define UseJsonSerialization

using Amazon.XRay.Recorder.Core;
using Enyim.Caching;
using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using ChatWeb.Helpers;
using Newtonsoft.Json;

namespace ChatWeb.Services
{
    public class MemCacheService : IDisposable
    {
        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IMemcachedClient Client;
        private readonly string memCacheConfigNode;
        bool disposed;

        public MemCacheService(string configNode)
        {
            memCacheConfigNode = configNode;

            if (!String.IsNullOrEmpty(memCacheConfigNode))
            {
                logger.InfoFormat("Config: Using external memcache '{0}'", memCacheConfigNode);

                var config = new MemcachedClientConfiguration();
                config.AddServer(memCacheConfigNode);

                Client = new MemcachedClient(config);
            }
            else
            {
                logger.InfoFormat("Config: Using local memory caching");

                // Use our fake memory client for local debugging, since we can't access AWS Elasticache outside of AWS
                Client = new MemoryCacheClient();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources.

                    if (Client != null)
                        Client.Dispose();
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }
            disposed = true;
        }

        public T GetObject<T>(string id)
        {
            if (!String.IsNullOrEmpty(id))
            {
                var data = GetValue(id);
                if (data != null)
                {
#if  UseJsonSerialization
                    return JsonConvert.DeserializeObject<T>(data);
#else
                    using (var stream = new MemoryStream(data))
                    {
                        return (T)new BinaryFormatter().Deserialize(stream);
                    }
#endif
                }
            }

            return default(T);
        }

        public bool SaveObject(string id, object value, int timeout)
        {
#if  UseJsonSerialization
            return StoreValue(id, JsonConvert.SerializeObject(value), timeout);
#else
            byte[] bytes;
            using (var stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, value);
                bytes = stream.ToArray();
            }

            return StoreValue(id, bytes, timeout);
#endif
        }

#if UseJsonSerialization
        string GetValue(string key)
#else
        byte[] GetValue(string key)
#endif
        {
            string subsegmentName = memCacheConfigNode ?? "local";

            if (AwsUtilityMethods.IsRunningOnAWS)
                BeginSubsegment("GET");

            try
            {
#if UseJsonSerialization
                return Client.Get<string>(key);
#else
                return Client.Get<byte[]>(key);
#endif
            }
            catch (Exception ex)
            {
                if (AwsUtilityMethods.IsRunningOnAWS)
                    AWSXRayRecorder.Instance.AddException(ex);
                logger.ErrorFormat("Network: Memcache Get Error. '{0}'", ex.Message);
                throw;
            }
            finally
            {
                if (AwsUtilityMethods.IsRunningOnAWS)
                    AWSXRayRecorder.Instance.EndSubsegment();
            }
        }

#if UseJsonSerialization
        bool StoreValue(string key, string value, int timeout)
#else
        bool StoreValue(string key, byte[] value, int timeout)
#endif
        {
            TimeSpan ChatTimeout = new TimeSpan(0, timeout, 0);
            var expires = DateTime.Now + ChatTimeout;

            if (AwsUtilityMethods.IsRunningOnAWS)
                BeginSubsegment("POST");

            try
            {
                return Client.Store(StoreMode.Set, key, value, expires);
            }
            catch (Exception ex)
            {
                if (AwsUtilityMethods.IsRunningOnAWS)
                    AWSXRayRecorder.Instance.AddException(ex);
                logger.ErrorFormat("Network: Memcache Store Error. '{0}'", ex.Message);
                throw;
            }
            finally
            {
                if (AwsUtilityMethods.IsRunningOnAWS)
                    AWSXRayRecorder.Instance.EndSubsegment();
            }
        }

        private void BeginSubsegment(string method)
        {
            string subsegmentName = memCacheConfigNode ?? "local";

            AWSXRayRecorder.Instance.BeginSubsegment(subsegmentName);
            AWSXRayRecorder.Instance.SetNamespace(String.IsNullOrEmpty(memCacheConfigNode) ? "local" : "remote");
            Dictionary<string, object> requestInformation = new Dictionary<string, object>
            {
                ["url"] = subsegmentName,
                ["method"] = method
            };
            AWSXRayRecorder.Instance.AddHttpInformation("request", requestInformation);
        }

        public void FlushAll()
        {
            Client.FlushAll();
        }
    }
}