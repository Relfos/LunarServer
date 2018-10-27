using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace LunarLabs.WebServer.Core
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
            var buffer = new byte[1024*64];

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

        public static string Summary(this string text, int wordCount)
        {
            var s = text.Split(' ');
            var sb = new StringBuilder();

            int max = Math.Min(wordCount, s.Length);
            for (int i = 0; i < max; i++)
            {
                if (i > 0) { sb.Append(' '); }
                sb.Append(s[i]);
            }

            sb.Append("...");
            return sb.ToString();
        }

        /*public static string Summary(this string text, int wordCount)
        {
            bool inTag = false;
            int cntr = 0;
            int cntrWords = 0;
            Char lastc = ' ';

            // loop through html, counting only viewable content
            foreach (Char c in text)
            {
                if (cntrWords == wordCount) break;
                cntr++;
                if (c == '<')
                {
                    inTag = true;
                    continue;
                }

                if (c == '>')
                {
                    inTag = false;
                    continue;
                }
                if (!inTag)
                {
                    // do not count double spaces, and a space not in a tag counts as a word
                    if (c == 32 && lastc != 32)
                        cntrWords++;
                }
            }

            string substr = text.Substring(0, cntr) + " ...";

            //search for nonclosed tags        
            MatchCollection openedTags = new Regex("<[^/](.|\n)*?>").Matches(substr);
            MatchCollection closedTags = new Regex("<[/](.|\n)*?>").Matches(substr);

            // create stack          
            Stack<string> opentagsStack = new Stack<string>();
            Stack<string> closedtagsStack = new Stack<string>();

            foreach (Match tag in openedTags)
            {
                string openedtag = tag.Value.Substring(1, tag.Value.Length - 2);
                // strip any attributes, sure we can use regex for this!
                if (openedtag.IndexOf(" ") >= 0)
                {
                    openedtag = openedtag.Substring(0, openedtag.IndexOf(" "));
                }

                // ignore brs as self-closed
                if (openedtag.Trim() != "br")
                {
                    opentagsStack.Push(openedtag);
                }
            }

            foreach (Match tag in closedTags)
            {
                string closedtag = tag.Value.Substring(2, tag.Value.Length - 3);
                closedtagsStack.Push(closedtag);
            }

            if (closedtagsStack.Count < opentagsStack.Count)
            {
                while (opentagsStack.Count > 0)
                {
                    string tagstr = opentagsStack.Pop();

                    if (closedtagsStack.Count == 0 || tagstr != closedtagsStack.Peek())
                    {
                        substr += "</" + tagstr + ">";
                    }
                    else
                    {
                        closedtagsStack.Pop();
                    }
                }
            }

            return substr;
        }*/

        // warning - this might not work in all cases, a better solution is necessary later
        public static string StripHTML(this string input)
        {
            return Regex.Replace(input, "<.*?>", String.Empty);
        }

    }
}
