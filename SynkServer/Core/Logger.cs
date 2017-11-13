using System;

namespace SynkServer.Core
{
    public class Logger
    {
        public static int level = 1;

        public static bool useColors = true;

        protected void Log(ConsoleColor c, string s)
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

        public void Debug(object message)
        {
            if (level > 0) return;
            Log(ConsoleColor.Cyan, message.ToString());
        }

        public void Info(object message)
        {
            if (level > 1) return;
            Log(ConsoleColor.White, message.ToString());
        }

        public void Warning(object message)
        {
            if (level > 2) return;
            Log(ConsoleColor.Yellow, message.ToString());
        }

        public void Error(object message)
        {
            if (level > 3) return;
            Log(ConsoleColor.Red, message.ToString());
        }

    }

}