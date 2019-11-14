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
    public class Fingerprint
    {
        public string DeviceId { get; set; }
        public string Created { get; set; }
        public string FirstSeen { get; set; }
        public string BrowserFingerprint { get; set; }
        public int BrowserFingerprintLength { get; set; }
        public FingerprintDevice Device { get; set; }
        public string PublicIp { get; set; }
        public bool IsTor { get; set; }
        public bool IsIncognito { get; set; }
        public bool IsBlockingAds { get; set; }
        public FingerprintFacebook Facebook { get; set; }
        public FingerprintGoogleanalytics GoogleAnalytics { get; set; }
        public FingerprintGeoip GeoIp { get; set; }
    }

    [Serializable]
    public class FingerprintDevice
    {
        public string Type { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public FingerprintClient Client { get; set; }
        public FingerprintOs Os { get; set; }
        public FingerprintBot Bot { get; set; }
    }

    [Serializable]
    public class FingerprintClient
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public float Version { get; set; }
        public string Engine { get; set; }
    }

    [Serializable]
    public class FingerprintOs
    {
        public string Name { get; set; }
        public float Version { get; set; }
        public string Platform { get; set; }
    }

    [Serializable]
    public class FingerprintBot
    {
    }

    [Serializable]
    public class FingerprintFacebook
    {
        public bool LoggedIn { get; set; }
        public string Carrier { get; set; }
    }

    [Serializable]
    public class FingerprintGoogleanalytics
    {
        public string ClientId { get; set; }
        public string Domain { get; set; }
    }

    [Serializable]
    public class FingerprintGeoip
    {
        public string Country { get; set; }
        public string CountryCode { get; set; }
        public string Region { get; set; }
        public string RegionCode { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
    }

}