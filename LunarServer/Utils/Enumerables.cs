using System.Collections;

namespace LunarLabs.WebServer.Utils
{
    static class EnumerableExtensions
    {
        public static int Count(this IEnumerable source)
        {
            int res = 0;

            foreach (var item in source)
                res++;

            return res;
        }

        public static bool Any(this IEnumerable source)
        {
            bool res = false;

            foreach (var item in source)
            {
                res = true;
                break;
            }

            return res;
        }
    }
}
