using System;
using LunarLabs.WebServer.HTTP;

namespace LunarLabs.WebServer.Utils
{
    public static class RequestExtensions
    {
        public static long GetLong(this HTTPRequest request, string name)
        {
            var str = request.GetVariable(name);
            long result;
            if (long.TryParse(str, out result))
            {
                return result;
            }

            return 0;
        }

        public static T GetEnum<T> (this HTTPRequest request, string name) where T: struct
        {
            var str = request.GetVariable(name);

            var type = typeof(T);
            T result;

            if (Enum.TryParse<T>(str, out result))
            {
                return (T)result;
            }

            return default(T);
        }
    }
}
