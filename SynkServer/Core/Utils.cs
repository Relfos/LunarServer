using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace SynkServer.Core
{
    public static class Utils
    {
        /// <summary>
        /// Compresses the specified buffer using the G-Zip compression algorithm.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <returns></returns>
        public static byte[] GZIPCompress(this byte[] buffer)
        {
            byte[] outputBuffer;

            using (var targetStream = new MemoryStream())
            {
                using (var compressor = new GZipStream(targetStream, CompressionMode.Compress, true))
                {
                    compressor.Write(buffer, 0, buffer.Length);
                }
                outputBuffer = targetStream.ToArray();
            }

            return outputBuffer;
        }

        public static List<string> ReadLines(this Socket client)
        {
            var buffer = new byte[2048];

            int ofs = 0;
            int left = buffer.Length;

            var sb = new StringBuilder();

            var lines = new List<string>();

            do
            {
                int index = ofs;

                int n = client.Receive(buffer, ofs, left, SocketFlags.None);
                if (n <= 0)
                {
                    return null;
                }

                ofs += n;
                left -= n;

                int prev = 0;

                for (int i = index; i < ofs; i++)
                {
                    int cur = buffer[i];

                    if (cur == 10 && prev == 13)
                    {
                        sb.Length--;
                        var str = sb.ToString();

                        if (str.Length == 0)
                        {
                            return lines;
                        }

                        lines.Add(str);
                        sb.Length = 0;
                    }
                    else
                    {
                        sb.Append((char)cur);
                    }

                    prev = cur;
                }
            } while (left >= 0);

            return lines;
        }

        public static string UrlDecode(this string text)
        {
            // pre-process for + sign space formatting since System.Uri doesn't handle it
            // plus literals are encoded as %2b normally so this should be safe
            text = text.Replace("+", " ");
            return System.Uri.UnescapeDataString(text);
        }

        public static string UrlEncode(this string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            text = Uri.EscapeDataString(text);

            // UrlEncode escapes with lowercase characters (e.g. %2f) but oAuth needs %2F
            text = Regex.Replace(text, "(%[0-9a-f][0-9a-f])", c => c.Value.ToUpper());

            // these characters are not escaped by UrlEncode() but needed to be escaped
            text = text
                .Replace("(", "%28")
                .Replace(")", "%29")
                .Replace("$", "%24")
                .Replace("!", "%21")
                .Replace("*", "%2A")
                .Replace("'", "%27");

            // these characters are escaped by UrlEncode() but will fail if unescaped!
            text = text.Replace("%7E", "~");

            return text;
        }

        public static string FixUrl(string url)
        {
            if (url.StartsWith("/"))
            {
                url = url.Substring(1);
            }

            if (url.EndsWith("/"))
            {
                url = url.Substring(0, url.Length - 1);
            }

            return url;
        }

        private const string UA = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)";

        public static string HTTPGet(string url, Dictionary<string, string> headers = null)
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

        public static string HTTPPost(string url, Dictionary<string, string> args = null, Dictionary<string, string> headers = null)
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
