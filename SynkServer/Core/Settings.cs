﻿using System;
using System.IO;

namespace SynkServer.Core
{
    public enum ServerEnvironment
    {
        Dev,
        Prod
    }

    public struct ServerSettings
    {
        public int port;
        public string path;
        public string host;
        public ServerEnvironment environment;

        public static ServerSettings Parse(string[] args)
        {
            var exePath = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);

            var result = new ServerSettings()
            {
                port = 80,
                path = exePath,
                host = "localhost",
                environment = ServerEnvironment.Dev
            };

            foreach (var arg in args)
            {
                if (!arg.StartsWith("--"))
                {
                    continue;
                }

                var temp = arg.Substring(2).Split(new char[] { '=' }, 2);
                var key = temp[0];
                var val = temp.Length > 1 ? temp[1] : "";

                switch (key)
                {
                    case "host": result.host = val; break;
                    case "port": int.TryParse(val, out result.port); break;
                    case "path":
                        {
                            result.path = val;
                            break;
                        }
                    case "env": Enum.TryParse(val.FirstLetterToUpper(), out result.environment); break;
                }
            }

            result.path = result.path.Replace("\\", "/");
            if (!result.path.EndsWith("/"))
            {
                result.path += "/";
            }

            return result;
        }
    }
}
