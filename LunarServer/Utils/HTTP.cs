using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace LunarLabs.WebServer.Utils
{
    public static class HTTPUtils
    {
        private const string UA = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)";

        public static string Get(string url, Dictionary<string, string> headers = null)
        {
            using (var wc = new WebClient())
            {
                wc.Encoding = System.Text.Encoding.UTF8;
                wc.Headers.Add("user-agent", UA);
                wc.Headers.Add("Content-Type", "application/json");

                if (headers != null)
                {
                    foreach (var entry in headers)
                    {
                        wc.Headers.Add(entry.Key, entry.Value);
                    }
                }

                string contents = "";
                try
                {
                    contents = wc.DownloadString(url);
                }
                catch (WebException ex)
                {
                    using (var stream = ex.Response.GetResponseStream())
                    using (var sr = new StreamReader(stream))
                    {
                        contents = sr.ReadToEnd();
                    }
                }
                return contents;
            }
        }

        public static string Post(string url, Dictionary<string, string> args = null, Dictionary<string, string> headers = null)
        {
            string myParameters = "";
            if (args != null)
            {
                foreach (var arg in args)
                {
                    if (myParameters.Length > 0) { myParameters += "&"; }
                    myParameters += arg.Key + "=" + arg.Value;
                }
            }

            using (WebClient wc = new WebClient())
            {
                wc.Encoding = System.Text.Encoding.UTF8;
                wc.Headers.Add("user-agent", UA);
                wc.Headers.Add("Content-Type", "application/json");

                wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";

                if (headers != null)
                {
                    foreach (var entry in headers)
                    {
                        wc.Headers.Add(entry.Key, entry.Value);
                    }
                }


                string contents = "";
                try
                {
                    contents = wc.UploadString(url, myParameters);
                }
                catch (WebException ex)
                {
                    using (var stream = ex.Response.GetResponseStream())
                    using (var sr = new StreamReader(stream))
                    {
                        contents = sr.ReadToEnd();
                    }
                }
                return contents;
            }
        }
    }
}
