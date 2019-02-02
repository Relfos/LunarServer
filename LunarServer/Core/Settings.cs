using System;

namespace LunarLabs.WebServer.Core
{
    public enum ServerEnvironment
    {
        Dev,
        Prod
    }

    public struct ServerSettings
    {
        public int Port;
        public string Path;
        public string Host;
        public bool Compression;
        public ServerEnvironment Environment;
        public int MaxPostSizeInBytes;
        public int CacheResponseTime;

        public static ServerSettings DefaultSettings()
        {
            var exePath = System.IO.Path.GetDirectoryName(System.Environment.GetCommandLineArgs()[0]);

            return new ServerSettings()
            {
                Port = 80,
                Compression = true,
                Path = exePath,
                Host = "localhost",
                MaxPostSizeInBytes = 1024 * 1024 * 8,
                CacheResponseTime = -1,
                Environment = ServerEnvironment.Dev
            };
        }

        public static ServerSettings Parse(string[] args)
        {
            var result = DefaultSettings();

            foreach (var arg in args)
            {
                if (!arg.StartsWith("--"))
                {
                    continue;
                }

                var temp = arg.Substring(2).Split(new char[] { '=' }, 2);
                var key = temp[0].ToLower();
                var val = temp.Length > 1 ? temp[1] : "";

                switch (key)
                {
                    case "host": result.Host = val; break;
                    case "port": int.TryParse(val, out result.Port); break;
                    case "postsize": int.TryParse(val, out result.MaxPostSizeInBytes); break;
                    case "compression": bool.TryParse(val, out result.Compression); break;
                    case "cachetime": int.TryParse(val, out result.CacheResponseTime); break;
                    case "path":
                        {
                            result.Path = System.IO.Path.GetFullPath(val);
                            break;
                        }
                    case "env": Enum.TryParse(val.FirstLetterToUpper(), out result.Environment); break;
                }
            }

            if (result.CacheResponseTime < 0)
            {
                result.CacheResponseTime = result.Environment == ServerEnvironment.Dev ? 0 : 5;
            }

            result.Path = result.Path.Replace("\\", "/");
            if (!result.Path.EndsWith("/"))
            {
                result.Path += "/";
            }

            return result;
        }
    }
}
