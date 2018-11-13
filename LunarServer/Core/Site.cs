using LunarLabs.Parser;
using LunarLabs.Parser.JSON;
using LunarLabs.WebServer.HTTP;
using System;
using System.Collections.Generic;
using System.IO;

namespace LunarLabs.WebServer.Core
{
    public abstract class SitePlugin
    {
        public Site Site { get; private set; }
        public string RootPath { get; private set; }

        public SitePlugin(Site site, string rootPath = null)
        {
            if (rootPath == null)
            {
                rootPath = "/";
            }

            this.Site = site;
            this.RootPath = rootPath;

            site.AddPlugin(this);
        }


        public abstract bool Install();

        public string Combine(string localPath)
        {
            if (string.IsNullOrEmpty(RootPath) || RootPath.Equals("/"))
            {
                return "/"+localPath;
            }

            return RootPath + "/" + localPath;
        }
    }

    public class Site
    {
        public string host { get { return server.settings.host; } }
        public Router router { get; private set; }
        public string filePath { get; private set; }

        public Logger log { get { return server.log; } }

        private AssetCache _cache;
        public FileCache Cache => _cache;

        public HTTPServer server { get; private set; }

        private List<SitePlugin> _plugins = new List<SitePlugin>();
        public IEnumerable<SitePlugin> plugins { get { return _plugins; } }

        public Site(HTTPServer server, string filePath)
        {
            this.server = server;

            if (string.IsNullOrEmpty(filePath))
            {
                this.filePath = null;
            }
            else
            {
                this.filePath = server.settings.path + filePath;
                if (!this.filePath.EndsWith("/"))
                {
                    this.filePath += "/";
                }
            }

            this.router = new Router();
            this._cache = new AssetCache(this, this.filePath);
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
            var route = router.Find(request.method, request.path, request.args);

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
                    return HTTPResponse.FromString((string)obj, HTTPCode.OK, true);
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

            _cache.Update();

            return _cache.GetFile(request);
        }
    }
}
