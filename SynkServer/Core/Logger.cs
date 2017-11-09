using System;

namespace SynkServer.Core
{
    public class Logger
    {
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

        public void Info(object message)
        {
            Log(ConsoleColor.White, message.ToString());
        }

        public void Error(object message)
        {
            Log(ConsoleColor.Red, message.ToString());
        }

        public void Warning(object message)
        {
            Log(ConsoleColor.Yellow, message.ToString());
        }
    }

}