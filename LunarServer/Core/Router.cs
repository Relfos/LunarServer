using LunarLabs.WebServer.HTTP;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace LunarLabs.WebServer.Core
{
    public struct RouteEndPoint
    {
        public readonly Func<HTTPRequest, object> Handler;
        public readonly int Priority;

        public RouteEndPoint(Func<HTTPRequest, object> handler, int priority)
        {
            Handler = handler;
            Priority = priority;
        }
    }

    public sealed class RouteEntry
    {
        public readonly string Route;
        public readonly List<RouteEndPoint> Handlers = new List<RouteEndPoint>();
        public readonly Dictionary<int, string> Names;

        public RouteEntry(string route, Dictionary<int, string> names)
        {
            this.Route = route;
            this.Names = names;
        }
    }
    public sealed class Router
    {
        private Dictionary<HTTPRequest.Method, Dictionary<string, RouteEntry>> _routes = new Dictionary<HTTPRequest.Method, Dictionary<string, RouteEntry>>();
        private Dictionary<HTTPRequest.Method, List<RouteEntry>> _wildRoutes = new Dictionary<HTTPRequest.Method, List<RouteEntry>>();
        public Router()
        {
            var methods = Enum.GetValues(typeof(HTTPRequest.Method)).Cast<HTTPRequest.Method>().ToArray();

            foreach (var method in methods)
            {
                _routes[method] = new Dictionary<string, RouteEntry>();
                _wildRoutes[method] = new List<RouteEntry>();
            }
        }

        public void Register(HTTPRequest.Method method, string path, int priority, Func<HTTPRequest, object> handler)
        {
            path = StringUtils.FixUrl(path);

            Dictionary<int, string> names;

            // TODO this probably only is necessary when creating a new RouteEntry
            if (path.Contains("{"))
            {
                string[] s = path.Split('/');
                var regex = new Regex(@"{([A-z]\w+)}");

                var sb = new StringBuilder();
                names = new Dictionary<int, string>();

                for (int i = 0; i < s.Length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append('/');
                    }

                    var match = regex.Match(s[i]);
                    if (match.Success)
                    {
                        sb.Append("*");
                        names[i] = match.Groups[1].Value;
                    }
                    else
                    {
                        sb.Append(s[i]);
                    }
                }

                path = sb.ToString();
            }
            else
            {
                names = null;
            }

            RouteEntry entry;

            if (path.Contains("*"))
            {
                entry = new RouteEntry(path, names);
                var list = _wildRoutes[method];

                if (list.Any(x => x.Route.Equals(path, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new ArgumentException($"Duplicated {method} path: " + path);
                }

                list.Add(entry);
            }
            else
            {
                var dic = _routes[method];

                if (dic.ContainsKey(path))
                {
                    entry = dic[path];
                }
                else
                {
                    entry = new RouteEntry(path, names);
                    dic[path] = entry;
                }
            }

            entry.Handlers.Add(new RouteEndPoint(handler, priority));
            entry.Handlers.Sort((x, y) => y.Priority.CompareTo(x.Priority));
        }

        public RouteEntry Find(HTTPRequest.Method method, string url, Dictionary<string, string> query)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            var table = _routes[method];

            if (!table.ContainsKey(url))
            {
                // first try wildcards (if any available)
                var list = _wildRoutes[method];

                foreach (var entry in list)
                {
                    if (StringUtils.MatchWildCard(url, entry.Route))
                    {
                        return entry;
                    }
                }

                // if still nothing found, try routes with args
                url = FindRouteWithArgs(method, url, query);

                if (url == null)
                {
                    return null;
                }
            }

            if (table.ContainsKey(url))
            {
                return table[url];
            }

            return null;
        }

        private string FindRouteWithArgs(HTTPRequest.Method method, string url, Dictionary<string, string> query)
        {
            //bool hasSlash = urlPath.Contains("/");
            var splitter = new char[] { '/' };

            string[] s = url.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();

            var table = _routes[method];

            var bitLen = 1 << s.Length;

            for (int i = 0; i < bitLen; i++)
            {
                sb.Length = 0;

                for (int j = 0; j < s.Length; j++)
                {
                    if (j > 0)
                    {
                        sb.Append('/');
                    }

                    bool isSet = (i & (1 << j)) != 0;
                    sb.Append(isSet ? s[j] : "*");
                }

                var path = sb.ToString();

                if (table.ContainsKey(path))
                {
                    var route = table[path];
                    
                    if (route.Names != null)
                    {
                        foreach (var entry in route.Names)
                        {
                            query[entry.Value] = s[entry.Key];
                        }
                    }

                    return path;
                }
            }

            return null;
        }


    }

}
