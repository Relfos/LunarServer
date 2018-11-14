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
        public string Host { get { return Server.Settings.host; } }
        public Router Router { get; private set; }
        public string FilePath { get; private set; }

        public Logger Logger { get { return Server.Logger; } }

        private AssetCache _cache;
        public FileCache Cache => _cache;

        public HTTPServer Server { get; private set; }

        private List<SitePlugin> _plugins = new List<SitePlugin>();
        public IEnumerable<SitePlugin> Plugins { get { return _plugins; } }

        public Site(HTTPServer server, string filePath)
        {
            this.Server = server;

            if (string.IsNullOrEmpty(filePath))
            {
                this.FilePath = null;
            }
            else
            {
                this.FilePath = server.Settings.path + filePath;
                if (!this.FilePath.EndsWith("/"))
                {
                    this.FilePath += "/";
                }
            }

            server.Site = this;
            this.Router = new Router();
            this._cache = new AssetCache(this, this.FilePath);
        }

        public virtual void Initialize()
        {
            foreach (var plugin in Plugins)
            {
                plugin.Install();
            }
        }

        public void Get(string path, Func<HTTPRequest, object> handler)
        {
            Router.Register(HTTPRequest.Method.Get, path, handler);
        }

        public void Post(string path, Func<HTTPRequest, object> handler)
        {
            Router.Register(HTTPRequest.Method.Post, path, handler);
        }

        public void Put(string path, Func<HTTPRequest, object> handler)
        {
            Router.Register(HTTPRequest.Method.Put, path, handler);
        }

        public void Delete(string path, Func<HTTPRequest, object> handler)
        {
            Router.Register(HTTPRequest.Method.Delete, path, handler);
        }

        public void AddPlugin(SitePlugin plugin)
        {
            this._plugins.Add(plugin);
        }

        public virtual HTTPResponse HandleRequest(HTTPRequest request)
        {
            Logger.Debug($"Router find {request.method}=>{request.url}");
            var route = Router.Find(request.method, request.path, request.args);

            if (route != null)
            {
                Logger.Debug("Calling route handler...");
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
                    return HTTPResponse.FromString((string)obj, HTTPCode.OK, Server.AutoCompress);
                }

                if (obj is byte[])
                {
                    return HTTPResponse.FromBytes((byte[])obj);
                }

                if (obj is DataNode)
                {
                    var root = (DataNode)obj;
                    var json = JSONWriter.WriteToString(root);
                    return HTTPResponse.FromString((string)obj, HTTPCode.OK, Server.AutoCompress, "application/json");
                }

                return null;
            }
            else
            {
                Logger.Debug("Route handler not found...");
            }

            _cache.Update();

            return _cache.GetFile(request);
        }
    }
}
