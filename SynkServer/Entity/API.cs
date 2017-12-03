using System;
using LunarParser;
using SynkServer.Core;
using SynkServer.HTTP;
using System.Collections.Generic;
using System.Linq;

namespace SynkServer.Entity
{
    public class APIRequestException : Exception
    {
        public APIRequestException(string message) : base(message)
        {

        }
    }

    public class EntityAPI: SitePlugin
    {
        private DataFormat format;
        private string mimeType;
        
        public EntityAPI(Site site, string rootPath = null, DataFormat format = DataFormat.JSON)  : base(site, rootPath)
        {
            this.format = format;
            this.mimeType = "application/" + format.ToString().ToLower();
        }

        public override bool Install()
        {
            return true;
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
                var handler = route.handler;

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
            var baseURL = Combine($"api/{name}");
            site.Get(baseURL, (request) => ListEntities<T>());
            site.Get(baseURL + "/new", (request) => NewEntity<T>(request));
            site.Get(baseURL + "/{id}/show", (request) => ShowEntity<T>(request));
            site.Get(baseURL + "/{id}/edit", (request) => EditEntity<T>(request));
            site.Get(baseURL + "/{id}/delete", (request) => DeleteEntity<T>(request));
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

        private static T Check<T>(string id) where T : Entity
        {
            var entity = Entity.FindById<T>(id);

            if (entity != null)
            {
                return entity;
            }

            throw new APIRequestException($"{typeof(T).Name} with '{id}' does not exist");
        }

        #region UTILS
        private static object ListEntities<T>() where T : Entity
        {
            var result = DataNode.CreateObject(typeof(T).Name.ToLower() + "s");

            foreach (var user in Entity.Every<T>())
            {
                var data = user.Serialize();
                result.AddNode(data);
            }

            return result;
        }

        private static object ShowEntity<T>(HTTPRequest requset) where T : Entity
       {
            var id = requset.args["id"];
            var entity = Entity.FindById<T>(id);
            var data = entity.Serialize();
            return data;
        }

        private static object NewEntity<T>(HTTPRequest request) where T : Entity
        {
            var entity = Entity.Create<T>();
            var node = request.args.ToDataSource(typeof(T).Name.ToLower());
            Check(request.args, Entity.GetFields<T>());
            entity.Deserialize(node);
            entity.Save();
            var data = entity.Serialize();
            return data;
        }

        private static object EditEntity<T>(HTTPRequest request) where T : Entity
        {
            var node = request.args.ToDataSource(typeof(T).Name.ToLower());
            var id = Check(request.args, "id");
            var entity = Check<T>(id);
            entity.Deserialize(node);
            entity.Save();
            var data = entity.Serialize();
            return data;
        }

        private static object DeleteEntity<T>(HTTPRequest request) where T : Entity
        {
            var node = request.args.ToDataSource(typeof(T).Name.ToLower());
            var id = Check(request.args, "id");
            var entity = Check<T>(id);
            entity.Delete();
            return OperationResult();
        }

        #endregion

    }
}