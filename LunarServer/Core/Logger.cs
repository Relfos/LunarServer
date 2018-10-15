using System;

namespace LunarLabs.WebServer.Core
{
    public struct LogEntry
    {
        public DateTime timestamp;
        public string text;
    }

    public enum LogLevel
    {
        Default,
        Debug,
        Info,
        Warning,
        Error
    }

    public abstract class Logger
    {
        public LogLevel level = LogLevel.Default;

        public bool useColors = true;

        protected abstract void Log(ConsoleColor c, string s);

        public void Debug(object message)
        {
            if (level > LogLevel.Debug) return;
            Log(ConsoleColor.Cyan, message.ToString());
        }

        public void Info(object message)
        {
            if (level > LogLevel.Info) return;
            Log(ConsoleColor.White, message.ToString());
        }

        public void Warning(object message)
        {
            if (level > LogLevel.Warning) return;
            Log(ConsoleColor.Yellow, message.ToString());
        }

        public void Error(object message)
        {
            if (level > LogLevel.Error) return;
            Log(ConsoleColor.Red, message.ToString());
        }
    }

    public class ConsoleLogger : Logger
    {
        protected override void Log(ConsoleColor c, string s)
        {
            //var isMono = Launcher.IsRunningOnMono();

            var temp = Console.ForegroundColor;
            if (useColors)
            {
                Console.ForegroundColor = c;
            }

            Console.WriteLine(s);

            if (useColors)
            {
                Console.ForegroundColor = temp;
            }
       }
    }

}