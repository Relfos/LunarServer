using System;
using LunarLabs.Parser;
using LunarLabs.WebServer.Core;
using LunarLabs.WebServer.HTTP;
using System.Collections.Generic;
using System.Linq;

namespace LunarLabs.WebServer.Entity
{
    public class APIRequestException : Exception
    {
        public APIRequestException(string message) : base(message)
        {

        }
    }

    public class EntityAPI: ServerPlugin
    {
        private DataFormat format;
        private string mimeType;

        public EntityStore Store { get; private set; }
        
        public EntityAPI(HTTPServer server, EntityStore store, string rootPath = null, DataFormat format = DataFormat.JSON)  : base(server, rootPath)
        {
            this.format = format;
            this.Store = store;
            this.mimeType = "application/" + format.ToString().ToLower();
        }

        protected HTTPResponse HandleRequest(HTTPRequest request)
        {
            var url = StringUtils.FixUrl(request.path);

            if (url.Equals("/favicon.ico"))
            {
                return null;
            }

            DataNode result;
            var query = request.args;

            RouteEntry route = null;// router.Find(url, query);

            if (route != null)
            {
                var handler = route.handlers.First().Key; // TODO fixme

                try
                {
                    result = (DataNode) handler(request);
                }
                catch (Exception ex)
                {
                    result = OperationResult(ex.Message);
                }

            }
            else
            {
                result = DataNode.CreateObject("result");
                result.AddField("error", "invalid route");
            }

            var response = new HTTPResponse();
            

            var content = DataFormats.SaveToString(format, result);
            response.bytes = System.Text.Encoding.UTF8.GetBytes(content);

            response.headers["Content-Type"] = mimeType;

            return response;
            
        }

        public static DataNode OperationResult(string errorMsg = null)
        {
            var result = DataNode.CreateObject("operation");
            result.AddField("result", string.IsNullOrEmpty(errorMsg) ? "success" : "error");
            if (!string.IsNullOrEmpty(errorMsg))
            {
                result.AddField("error", errorMsg);
            }
            return result;
        }

        public void GenerateAPI<T>() where T : Entity
        {
            var name = typeof(T).Name.ToLower() + "s";
            var baseURL =  $"{this.Path}api /{name}";
            Server.Get(baseURL, (request) => ListEntities<T>());
            Server.Get(baseURL + "/new", (request) => NewEntity<T>(request));
            Server.Get(baseURL + "/{id}/show", (request) => ShowEntity<T>(request));
            Server.Get(baseURL + "/{id}/edit", (request) => EditEntity<T>(request));
            Server.Get(baseURL + "/{id}/delete", (request) => DeleteEntity<T>(request));
        }

        private static void Check(Dictionary<string, string> dic, IEnumerable<string> args)
        {
            foreach (var arg in args)
            {
                if (!dic.ContainsKey(arg))
                {
                    throw new APIRequestException($"argument '{arg}' is required");
                }
            }
        }

        private static string Check(Dictionary<string, string> dic, string name)
        {
            if (dic.ContainsKey(name))
            {
                return dic[name];
            }

            throw new APIRequestException($"argument '{name}' is required");
        }

        private T Check<T>(string id) where T : Entity
        {
            var entity = Store.FindById<T>(id);

            if (entity != null)
            {
                return entity;
            }

            throw new APIRequestException($"{typeof(T).Name} with '{id}' does not exist");
        }

        #region UTILS
        private object ListEntities<T>() where T : Entity
        {
            var result = DataNode.CreateObject(typeof(T).Name.ToLower() + "s");

            foreach (var user in Store.Every<T>())
            {
                var data = user.Serialize();
                result.AddNode(data);
            }

            return result;
        }

        private object ShowEntity<T>(HTTPRequest requset) where T : Entity
       {
            var id = requset.args["id"];
            var entity = Store.FindById<T>(id);
            var data = entity.Serialize();
            return data;
        }

        private object NewEntity<T>(HTTPRequest request) where T : Entity
        {
            var entity = Store.Create<T>();
            var node = request.args.ToDataNode(typeof(T).Name.ToLower());
            Check(request.args, Entity.GetFields<T>());
            entity.Deserialize(node);
            entity.Save();
            var data = entity.Serialize();
            return data;
        }

        private  object EditEntity<T>(HTTPRequest request) where T : Entity
        {
            var node = request.args.ToDataNode(typeof(T).Name.ToLower());
            var id = Check(request.args, "id");
            var entity = Check<T>(id);
            entity.Deserialize(node);
            entity.Save();
            var data = entity.Serialize();
            return data;
        }

        private object DeleteEntity<T>(HTTPRequest request) where T : Entity
        {
            var node = request.args.ToDataNode(typeof(T).Name.ToLower());
            var id = Check(request.args, "id");
            var entity = Check<T>(id);
            entity.Delete();
            return OperationResult();
        }

        #endregion

    }
}