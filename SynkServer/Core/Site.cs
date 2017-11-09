using LunarParser;
using LunarParser.JSON;
using SynkServer.HTTP;
using System;
using System.Collections.Generic;
using System.IO;

namespace SynkServer.Core
{
    public class Site
    {
        public Router router { get; private set; }
        public string path { get; private set; }

        public Site(string path)
        {
            this.router = new Router();
            this.path = path;
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

        protected virtual HTTPResponse HandleRequest(HTTPRequest request)
        {
            var route = router.Find(request.method, request.url, request.args);

            if (route != null)
            {
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

            var fileName = path + request.url;
            if (File.Exists(fileName))
            {
                return HTTPResponse.FromFile(fileName);
            }

            return null;
        }
    }
}
