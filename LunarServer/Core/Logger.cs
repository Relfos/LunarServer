using System;

namespace LunarLabs.WebServer.Core
{
    public enum LogLevel
    {
        Default,
        Debug,
        Info,
        Warning,
        Error
    }

    public delegate void LoggerCallback(LogLevel level, string text);

    public static class ConsoleLogger
    {
        public static LogLevel MaxLevel = LogLevel.Default;

        public static bool useColors = true;

        public static void Write(LogLevel level, string text)
        {
            if (MaxLevel > level) return;

            var temp = Console.ForegroundColor;

            if (useColors)
            {
                ConsoleColor c;
                switch (level)
                {
                    case LogLevel.Debug: c = ConsoleColor.Cyan; break;
                    case LogLevel.Error: c = ConsoleColor.Red; break;
                    case LogLevel.Warning: c = ConsoleColor.Yellow; break;
                    default: c = ConsoleColor.Gray; break;
                }
                Console.ForegroundColor = c;
            }

            Console.WriteLine(text);

            if (useColors)
            {
                Console.ForegroundColor = temp;
            }
        }
    }
}