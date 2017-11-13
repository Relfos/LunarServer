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
        public Site site { get; protected set; }

        public abstract bool Install(Site site, string path);

        public string Combine(string rootPath, string localPath)
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
        public Router router { get; private set; }
        public string filePath { get; private set; }

        public Analytics analytics { get; private set; }

        public Logger log { get; private set; }

        public Site(Logger log, string path)
        {
            this.log = log;
            this.filePath = path;
            this.router = new Router();
            this.analytics = new Analytics(this);
        }

        public void Run(HTTPServer server)
        {
            server.Run(HandleRequest);
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

        public bool Install(SitePlugin plugin, string path = "/")
        {
            return plugin.Install(this, path);
        }

        protected virtual HTTPResponse HandleRequest(HTTPRequest request)
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
                    return HTTPResponse.FromBytes(bytes, true, "application/json");
                }

                return null;
            }
            else
            {
                log.Debug("Route handler not found...");
            }

            var fileName = filePath + request.url;
            if (File.Exists(fileName))
            {
                log.Debug($"Returning static file...{fileName}");
                return HTTPResponse.FromFile(fileName);
            }

            log.Warning("Nothing found...");
            return null;
        }
    }
}
