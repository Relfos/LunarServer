using LunarLabs.WebServer.HTTP;
using LunarLabs.WebServer.Templates;
using System.Net;

namespace LunarLabs.WebServer.Utils
{
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

    public static class DetectionUtils
    {
        private static string[] uaHttpHeaders = {
            // The default User-Agent string.
            "User-Agent",
            // Header can occur on devices using Opera Mini.
            "X-OperaMini-Phone-UA",
            // Vodafone specific header: http://www.seoprinciple.com/mobile-web-community-still-angry-at-vodafone/24/
            "X-Device-User-Agent",
            "X-Original-User-Agent",
        };

        public static OSType DetectOS(this HTTPRequest request)
        {
            foreach (var ua in uaHttpHeaders)
            {
                if (!request.headers.ContainsKey(ua))
                {
                    continue;
                }

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

        public static string DetectCountry(this HTTPRequest request)
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

        const string LanguageHeader = "Accept-Language";

        public static string DetectLanguage(this HTTPRequest request)
        {
            if (request.headers.ContainsKey(LanguageHeader))
            {
                var languages = request.headers[LanguageHeader].Split(new char[] { ',', ';' });
                foreach (var lang in languages)
                {
                    string code;
                    if (lang.Contains("-"))
                    {
                        code = lang.Split('-')[0];
                    }
                    else
                    {
                        code = lang;
                    }

                    if (LocalizationManager.HasLanguage(code))
                    {
                        return code;
                    }
                }
            }

            return "en";
        }

    }
}
