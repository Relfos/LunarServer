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

        public static string DownloadString(string url)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = "GET";
            try
            {
                using (WebResponse webResponse = webRequest.GetResponse())
                {
                    Stream str = webResponse.GetResponseStream();
                    using (StreamReader sr = new StreamReader(str))
                        return sr.ReadToEnd();
                }
            }
            catch (WebException wex)
            {
                using (HttpWebResponse response = (HttpWebResponse)wex.Response)
                {
                    Stream str = response.GetResponseStream();
                    if (str == null)
                        throw;

                    using (StreamReader sr = new StreamReader(str))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }

        }
    }
}
