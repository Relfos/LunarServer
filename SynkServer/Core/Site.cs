using LunarParser;
using LunarParser.JSON;
using SynkServer.HTTP;
using System;
using System.Collections.Generic;
using System.IO;

namespace SynkServer.Core
{
    public abstract class SitePlugin
    {
        public Site site { get; private set; }
        public string rootPath { get; private set; }

        public SitePlugin(Site site, string rootPath = null)
        {
            if (rootPath == null)
            {
                rootPath = "/";
            }

            this.site = site;
            this.rootPath = rootPath;
            this.Install();
        }


        public abstract bool Install();

        public string Combine(string localPath)
        {
            if (string.IsNullOrEmpty(rootPath) || rootPath.Equals("/"))
            {
                return localPath;
            }

            return rootPath + "/" + localPath;
        }
    }

    public class Site
    {
        public string host { get; private set; }
        public Router router { get; private set; }
        public string filePath { get; private set; }

        public Analytics analytics { get; private set; }

        public Logger log { get { return server.log; } }

        public Cache cache { get; private set; }

        public HTTPServer server { get; private set; }

        private List<SitePlugin> _plugins = new List<SitePlugin>();
        public IEnumerable<SitePlugin> plugins { get { return _plugins; } }

        public Site(HTTPServer server, string host, string filePath)
        {
            this.server = server;

            this.host = host;
            this.filePath = filePath;
            this.router = new Router();
            this.analytics = new Analytics(this);
            this.cache = new Cache(log, filePath);

            server.AddSite(this);
        }

        public virtual void Initialize()
        {
            foreach (var plugin in plugins)
            {
                plugin.Install();
            }
        }

        public void Get(string path, Func<HTTPRequest, object> handler)
        {
            router.Register(HTTPRequest.Method.Get, path, handler);
        }

        public void Post(string path, Func<HTTPRequest, object> handler)
        {
            router.Register(HTTPRequest.Method.Post, path, handler);
        }

        public void Put(string path, Func<HTTPRequest, object> handler)
        {
            router.Register(HTTPRequest.Method.Put, path, handler);
        }

        public void Delete(string path, Func<HTTPRequest, object> handler)
        {
            router.Register(HTTPRequest.Method.Delete, path, handler);
        }

        public void AddPlugin(SitePlugin plugin)
        {
            this._plugins.Add(plugin);
        }

        public virtual HTTPResponse HandleRequest(HTTPRequest request)
        {
            log.Debug($"Router find {request.method}=>{request.url}");
            var route = router.Find(request.method, request.url, request.args);

            if (route != null)
            {
                log.Debug("Calling route handler...");
                var obj = route.handler(request);
                
                if (obj == null)
                {
                    return null;
                }

                if (obj is HTTPResponse)
                {
                    return (HTTPResponse)obj;
                }

                if (obj is string)
                {
                    return HTTPResponse.FromString((string)obj);
                }

                if (obj is byte[])
                {
                    return HTTPResponse.FromBytes((byte[])obj);
                }

                if (obj is DataNode)
                {
                    var root = (DataNode)obj;
                    var json = JSONWriter.WriteToString(root);
                    var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                    return HTTPResponse.FromBytes(bytes, "application/json");
                }

                return null;
            }
            else
            {
                log.Debug("Route handler not found...");
            }

            return cache.GetFile(request);
        }
    }
}
