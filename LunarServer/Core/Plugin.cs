using LunarLabs.Parser;
using System;
using LunarLabs.WebServer.HTTP;
using System.Linq;

namespace LunarLabs.WebServer.Core
{
    [AttributeUsage(AttributeTargets.Method)]
    public class EndPointAttribute: Attribute
    {
        public readonly string Path;
        public readonly HTTPRequest.Method Method;
        public readonly int Priority;

        public EndPointAttribute(HTTPRequest.Method method, string path, int priority = 0)
        {
            this.Method = method;
            this.Path = path;
            this.Priority = priority;
        }
    }

    public abstract class ServerPlugin
    {
        public readonly HTTPServer Server;
        public readonly string Path;

        public ServerPlugin(HTTPServer server, string path = null)
        {
            if (path == null)
            {
                path = "/";
            }

            if (!path.EndsWith("/"))
            {
                path += "/";
            }

            this.Server = server;
            this.Path = path;

            server.AddPlugin(this);
        }

        internal bool Install()
        {
            var type = this.GetType();
            var methods = type.GetMethods().Where(m => m.GetCustomAttributes(typeof(EndPointAttribute), false).Length > 0).ToArray();

            foreach (var method in methods)
            {
                var attr = (EndPointAttribute) method.GetCustomAttributes(typeof(EndPointAttribute), false).FirstOrDefault();
                var fullPath = this.Path + attr.Path;

                var handler = (Func<HTTPRequest, object>)Delegate.CreateDelegate(typeof(Func<HTTPRequest, object>), this, method);

                Server.RegisterHandler(attr.Method, fullPath, attr.Priority, handler);
            }

            return OnInstall();
        }

        protected virtual bool OnInstall()
        {
            return true;
        }
    }
}
