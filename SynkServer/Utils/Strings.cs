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
    public static class StringUtils
    {
        public static object GetDefault(this Type t)
        {
            Func<object> f = GetDefault<object>;
            return f.Method.GetGenericMethodDefinition().MakeGenericMethod(t).Invoke(null, null);
        }

        private static T GetDefault<T>()
        {
            return default(T);
        }

        public static long ToTimestamp(this DateTime value)
        {
            long epoch = (value.Ticks - 621355968000000000) / 10000000;
            return epoch;
        }

        public static DateTime ToDateTime(this long unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public static string MD5(this string input)
        {
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            return inputBytes.MD5();
        }

        public static string MD5(this byte[] inputBytes)
        {
            // step 1, calculate MD5 hash from input
            var md5 = System.Security.Cryptography.MD5.Create();

            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }

            return sb.ToString();
        }

        public static string Base64Encode(this byte[] bytes)
        {
            return System.Convert.ToBase64String(bytes);
        }

        public static byte[] Base64Decode(this string base64EncodedData)
        {
            return System.Convert.FromBase64String(base64EncodedData);
        }

        public static string Base64UrlSanitize(this string s)
        {
            s = s.Replace("=", "%3D");
            s = s.Replace("+", "%2B");
            s = s.Replace("/", "%2F");
            return s;
        }


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

        public static bool ReadLines(this Socket client, out List<string> lines, out byte[] unread)
        {
            var buffer = new byte[2048];

            int ofs = 0;
            int left = buffer.Length;

            var sb = new StringBuilder();

            lines = new List<string>();
            unread = null;

            do
            {
                int index = ofs;

                int n = client.Receive(buffer, ofs, left, SocketFlags.None);
                if (n <= 0)
                {
                    return false;
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
                            unread = new byte[ofs - (i + 1)];
                            Array.Copy(buffer, i + 1, unread, 0, unread.Length);
                            return true;
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

            return true;
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

        public static string FirstLetterToUpper(this string str)
        {
            if (str == null)
                return null;

            if (str.Length > 1)
                return char.ToUpper(str[0]) + str.Substring(1);

            return str.ToUpper();
        }

    }
}
