using SynkServer.HTTP;
using System;
using System.Collections.Generic;
using System.Net;

namespace SynkServer.Core
{
    public class Session
    {
        private Dictionary<string, object> _data = new Dictionary<string, object>();

        public IEnumerable<KeyValuePair<string, object>> data { get { return _data; } }

        public string ID;
        public DateTime lastActivity;

        public Session(HTTPRequest request, string ID = null)
        {
            this.ID = ID != null ? ID : Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + DateTime.Now.Millisecond.ToString() + DateTime.Now.Ticks.ToString()));
            this.lastActivity = DateTime.Now;

            this.OS = DetectOS(request);
            this.country = DetectCountry(request);
        }

        public bool Contains(string name)
        {
            return _data.ContainsKey(name);
        }

        public object Get(string name)
        {
            if (_data.ContainsKey(name))
            {
                return _data[name];
            }

            return null;
        }

        public void Set(string name, object value)
        {
            _data[name] = value;
        }

        public void Remove(string name)
        {
            if (_data.ContainsKey(name))
            {
                _data.Remove(name);
            }            
        }

        public void Destroy()
        {
            _data.Clear();
        }

        public enum OSType
        {
            Unknown,
            Windows,
            Linux,
            OSX,
            Android,
            iOS,
            WindowsPhone,
            XboxOne,
            Playstation4,
        }

        public OSType OS { get; private set; }

        public string country;

        private static string[] uaHttpHeaders = {
            // The default User-Agent string.
            "User-Agent",
            // Header can occur on devices using Opera Mini.
            "X-OperaMini-Phone-UA",
            // Vodafone specific header: http://www.seoprinciple.com/mobile-web-community-still-angry-at-vodafone/24/
            "X-Device-User-Agent",
            "X-Original-User-Agent",
        };

        public OSType DetectOS(HTTPRequest request)
        {
            foreach (var ua in uaHttpHeaders)
            {
                var value = request.headers[ua];
                if (value == null)
                {
                    continue;
                }

                if (value.Contains("Windows Phone")) { return OSType.WindowsPhone; }

                if (value.Contains("Android")) { return OSType.Android; }

                if (value.Contains("AppleTV") || value.Contains("iPhone") || value.Contains("iPad")) { return OSType.iOS; }

                if (value.Contains("Xbox One")) { return OSType.XboxOne; }

                if (value.Contains("PlayStation 4")) { return OSType.Playstation4; }

                if (value.Contains("Win64") || value.Contains("WOW64")) { return OSType.Windows; }

                if (value.Contains("OS X")) { return OSType.OSX; }

                if (value.Contains("Linux")) { return OSType.Linux; }
            }

            return OSType.Unknown;
        }

        private static string[] ipHttpHeaders = {
            "Remote-Addr",
            "X_Forwarded_For",
            "Client-Ip",
            "X-Forwarded",
            "X-Cluster-Client-Ip",
            "Forwarded-For",
            "Forwarded"
        };

        public string DetectCountry(HTTPRequest request)
        {
            foreach (var ipHeader in ipHttpHeaders)
            {
                if (!request.headers.ContainsKey(ipHeader))
                {
                    continue;
                }

                var value = request.headers[ipHeader];
                if (value == null)
                {
                    continue;
                }

                var ipAddress = IPAddress.Parse(value);
                var ipBytes = ipAddress.GetAddressBytes();
                uint ip = (uint)ipBytes[0] << 24;
                ip += (uint)ipBytes[1] << 16;
                ip += (uint)ipBytes[2] << 8;
                ip += (uint)ipBytes[3];

                return CountryUtils.IPToCountry(ip);
            }

            return "";
        }

    }

}
