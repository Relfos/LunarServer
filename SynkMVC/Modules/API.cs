using LunarParser;
using LunarParser.JSON;
using SynkMVC;
using SynkMVC.Model;
using SynkMVC.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace SynkMVC.Modules
{
    public class API : Module
    {
        public override bool CheckPermissions(SynkContext context, User user, string action)
        {
            return true;
        }

        public override void OnInvalidAction(SynkContext context, string action)
        {
            var content = DataNode.CreateObject();
            var error = true;
            content.AddField("error", "method "+action+" not supported");
            APIResult(context, content, error);
        }

        public void OnDelete(SynkContext context)
        {
            var error = false;
            var content = DataNode.CreateObject();

            if (context.request.HasVariable("entity"))
            {
                var entityClass = context.request.GetVariable("entity");

                if (context.request.HasVariable("id"))
                {
                    long entityID;
                    long.TryParse(context.request.GetVariable("id"), out entityID);
                    var entity = context.database.FetchEntityByID(entityClass, entityID);
                    if (entity.exists)
                    {
                        content.AddField("id", entityID.ToString());
                        entity.Remove(context);
                    }
                    else
                    {
                        error = true;
                        content.AddField("error", "ID does not exist");
                    }
                }
                else
                {
                    error = true;
                    content.AddField("error", "ID is required");
                }
            }
            else
            {
                error = true;
                content.AddField("error", "Entity type not specified");
            }

            APIResult(context, content, error);
        }

        public void OnInsert(SynkContext context)
        {
            var error = false;
            var content = DataNode.CreateObject();

            if (context.request.HasVariable("entity"))
            {
                var entityClass = context.request.GetVariable("entity");
                var entity = context.database.CreateEntity(entityClass);

                foreach (var field in entity.fields)
                {
                    var fieldName = field.name;

                    if (context.request.HasVariable(fieldName))
                    {
                        entity.SetFieldValue(fieldName, context.request.GetVariable(fieldName));
                    }
                    else
                    if (!field.required)
                    {
                        entity.SetFieldValue(fieldName, field.GetDefaultValue(context));
                    }
                    else
                    {
                        error = true;
                        content.AddField("error", fieldName + " is required");
                        break;
                    }
                }

                if (!error)
                {
                    entity.Save(context);
                    content.AddField("id", entity.id.ToString());
                }
            }
            else
            {
                error = true;
                content.AddField("error", "Entity type not specified");
            }

            APIResult(context, content, error);
        }

        public void OnGet(SynkContext context)
        {
            var error = false;
            var content = DataNode.CreateObject();

            if (context.request.HasVariable("entity"))
            {
                var entityClass = context.request.GetVariable("entity");
                if (context.request.HasVariable("id"))
                {
                    long entityID;
                    long.TryParse(context.request.GetVariable("id"), out entityID);

                    var obj = DataNode.CreateObject("content");
                    content.AddNode(obj);

                    var entity = context.database.FetchEntityByID(entityClass, entityID);
                    if (entity.exists)
                    {
                        obj.AddField("id", entity.id.ToString());
                        var fields = entity.GetFields();
                        obj.AddNode(fields.ToDataSource());
                    }
                    else
                    {
                        error = true;
                        content.AddField("error", "ID does not exist");
                    }
                }
                else
                {
                    error = true;
                    content.AddField("error", "id is required for");
                }
            }
            else
            {
                error = true;
                content.AddField("error", "Entity type not specified");
            }

            APIResult(context, content, error);
        }

        public void OnList(SynkContext context)
        {
            var error = false;
            var content = DataNode.CreateObject();

            if (context.request.HasVariable("entity"))
            {
                var entityClass = context.request.GetVariable("entity");
                var entities = context.database.FetchAllEntities(entityClass);

                var obj = DataNode.CreateObject("content");
                content.AddNode(obj);

                foreach (var entity in entities)
                {
                    var fields = entity.GetFields();
                    var child = DataNode.CreateObject("entity");
                    obj.AddNode(child);
                    child.AddField("id", entity.id.ToString());
                    child.AddNode(fields.ToDataSource());                    
                }
            }
            else
            {
                error = true;
                content.AddField("error", "Entity type not specified");
            }

            APIResult(context, content, error);
        }

        private void APIResult(SynkContext context, DataNode content, bool error)
        {
            var result = DataNode.CreateObject();
            result.AddField("result", error ? "error" : "ok");
            result.AddNode(content);

            var json = JSONWriter.WriteToString(result);
            context.Echo(json);
        }

    }
}