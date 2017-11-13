using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SynkMVC
{
    public static class Utility
    {
        public static string FixNewLines(this string s)
        {
            return s.Replace(System.Environment.NewLine, "<br>");
        }

        public static string Shuffle(this string str)
        {
            char[] array = str.ToCharArray();
            Random rng = new Random();
            int n = array.Length;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                var value = array[k];
                array[k] = array[n];
                array[n] = value;
            }
            return new string(array);
        }

        public static string Summary(this string text, int wordCount)
        {
            var s = text.Split(' ');
            var sb = new StringBuilder();

            int max = Math.Min(wordCount, s.Length);
            for (int i=0; i<max; i++)
            {
                if (i>0) { sb.Append(' '); }
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

        public static string BytesToString(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        public static byte[] StringToBytes(string src)
        {
            return System.Text.Encoding.UTF8.GetBytes(src);
        }

        public static byte[] Compress(string src)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(src);
            return Compress(bytes);
        }

        public static void CopyTo(this Stream input, Stream output)
        {
            byte[] buffer = new byte[16 * 1024]; // Fairly arbitrary size
            int bytesRead;

            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, bytesRead);
            }
        }

        public static byte[] Compress(byte[] srcData)
        {
            MemoryStream input = new MemoryStream(srcData);

            using (var compressStream = new MemoryStream())
            using (var compressor = new DeflateStream(compressStream, CompressionMode.Compress))
            {
                CopyTo(input, compressor);
                compressor.Close();
                return compressStream.ToArray();
            }
        }

        public static byte[] Decompress(byte[] data, bool skipHeader = false)
        {
            var output = new MemoryStream();
            using (var compressedStream = new MemoryStream(data))
            {
                if (skipHeader)
                {
                    compressedStream.Seek(2, SeekOrigin.Begin); //fix for php
                }

                using (var zipStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
                {
                    CopyTo(zipStream, output);
                    zipStream.Close();
                    output.Position = 0;
                    return output.ToArray();
                }

            }

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

        public static string GetMD5(string fileName)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                using (var stream = File.OpenRead(fileName))
                {
                    var hash = md5.ComputeHash(stream);

                    // step 2, convert byte array to hex string
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hash.Length; i++)
                    {
                        sb.Append(hash[i].ToString("X2"));
                    }

                    return sb.ToString();
                }
            }
        }

        public static string GetUniqID()
        {
            var ts = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
            double t = ts.TotalMilliseconds / 1000;

            int a = (int)Math.Floor(t);
            int b = (int)((t - Math.Floor(t)) * 1000000);

            return a.ToString("x8") + b.ToString("x5");
        }

        public static string FirstLetterToUpper(this string str)
        {
            if (str == null)
                return null;

            if (str.Length > 1)
                return char.ToUpper(str[0]) + str.Substring(1);

            return str.ToUpper();
        }

        static readonly string[] SizeSuffixes =  { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        public static string SizeSuffix(Int64 value, int decimalPlaces = 1)
        {
            if (value < 0) { return "-" + SizeSuffix(-value); }
            if (value == 0) { return "0.0 bytes"; }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
        }

    }
}
