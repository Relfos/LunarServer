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

        public void DebugFormat(string format, params object[] args)
        {
            Log(ConsoleColor.White, String.Format(format, args));
        }

        public void Error(object message)
        {
            Log(ConsoleColor.Red, message.ToString());
        }

        public void Error(object message, Exception exception)
        {
            Log(ConsoleColor.Red, message.ToString());
        }


        public void ErrorFormat(string format, params object[] args)
        {
            Log(ConsoleColor.Red, string.Format(format, args));
        }

        public void Info(object message)
        {
            Log(ConsoleColor.Green, message.ToString());
        }


        public void InfoFormat(string format, params object[] args)
        {
            Log(ConsoleColor.Green, string.Format(format, args));
        }


        public void WarnFormat(string format, params object[] args)
        {
            Log(ConsoleColor.Yellow, string.Format(format, args));
        }


}

}