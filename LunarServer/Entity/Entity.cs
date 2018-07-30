using LunarLabs.Parser;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LunarLabs.WebServer.Entity
{
    public abstract class Entity
    {
        public string id;

        public EntityStore Store { get; private set; }

        internal void SetStore(EntityStore store)
        {
            this.Store = store;
        }

        public void Delete()
        {
            var type = this.GetType();
            var collection = Store.GetCollection(type);
            collection.DeleteObject(this.id);
            Store.RequestBackgroundThread();
        }

        public void Save()
        {
            var type = this.GetType();
            var collection = Store.GetCollection(type);
            collection.RequestSave();
            Store.RequestBackgroundThread();
        }

        public static IEnumerable<String> GetFields<T>() where T: Entity
        {
            Type type = typeof(T);
            return type.GetFields().Where(f => f.IsPublic).Select(x => x.Name);
        }

        public DataNode Serialize()
        {
            Type type = this.GetType();
            var result = DataNode.CreateObject(type.Name.ToLowerInvariant());

            var fields = type.GetFields().Where(f => f.IsPublic);
            foreach (var field in fields)
            {
                var val = field.GetValue(this);
                if (val != null) {
                    result.AddField(field.Name.ToLowerInvariant(), val);
                }                
            }

            return result;
        }

        public void Deserialize(DataNode node)
        {
            Type type = this.GetType();

            var fields = type.GetFields().Where(f => f.IsPublic);
            foreach (var field in fields)
            {
                if (!node.HasNode(field.Name))
                {
                    continue;
                }

                var fieldType = field.FieldType;
                if (fieldType == typeof(DateTime))
                {
                    var val = node.GetDateTime(field.Name);
                    field.SetValue(this, val);
                }
                else
                if (fieldType == typeof(bool))
                {
                    var val = node.GetBool(field.Name);
                    field.SetValue(this, val);
                }
                else
                if (fieldType == typeof(int))
                {
                    var val = node.GetInt32(field.Name);
                    field.SetValue(this, val);
                }
                else
                if (fieldType.IsEnum)
                {
                    var temp = node.GetString(field.Name);
                    var values = Enum.GetValues(fieldType).Cast<int>().ToArray();
                    var names = Enum.GetNames(fieldType).Cast<string>().ToArray();
                    for (int i=0; i<names.Length; i++)
                    {
                        if (names[i].Equals(temp, StringComparison.OrdinalIgnoreCase))
                        {
                            field.SetValue(this, values[i]);
                            break;
                        }
                    }                    
                }
                else
                {
                    var val = node.GetString(field.Name);
                    field.SetValue(this, val);
                }                
            }
        }


    }
}
