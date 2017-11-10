using LunarParser;
using LunarParser.XML;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace SynkServer.Entity
{
    public class EntityCollection
    {
        public Type objectType { get; private set;}

        public EntityCollection(Type objectType)
        {
            this.objectType = objectType;

            if (File.Exists(GetFileName()))
            {
                var root = XMLReader.ReadFromString(File.ReadAllText(GetFileName()));
                var name = objectType.Name.ToLower();

                root = root["collection"];

                foreach (var child in root.Children)
                {
                    if (!child.Name.Equals(name))
                    {
                        continue;
                    }

                    var obj = AllocObject();
                    obj.Deserialize(child);
                    _objects[obj.ID] = obj;
                }
            }
        }

        private string GetFileName()
        {
            return objectType.Name + ".xml";
        }

        private Dictionary<string, Entity> _objects = new Dictionary<string, Entity>();
        public IEnumerable<Entity> Objects { get { return _objects.Values; } }

        private Entity AllocObject()
        {
            lock (this)
            {
                var obj = (Entity)Activator.CreateInstance(objectType);
                return obj;
            }
        }

        public Entity CreateObject()
        {
            var obj = AllocObject();
            obj.ID = Guid.NewGuid().ToString();
            _objects[obj.ID] = obj;
            return obj;
        }

        public Entity FindObject(string ID)
        {
            lock (this)
            {
                if (_objects.ContainsKey(ID))
                {
                    return _objects[ID];
                }
                return null;
            }
        }

        public void DeleteObject(string ID)
        {
            if (_objects.ContainsKey(ID))
            {
                _objects.Remove(ID);
            }
        }

        public void Save()
        {
            lock (this)
            {
                var result = DataNode.CreateObject("collection");
                foreach (var obj in _objects.Values)
                {
                    var node = obj.Serialize();
                    result.AddNode(node);
                }

                var contents = XMLWriter.WriteToString(result);
                File.WriteAllText(GetFileName(), contents);
                _shouldSave = false;
            }
        }

        private bool _shouldSave = false;

        public void RequestSave()
        {
            lock (this)
            {
                _shouldSave = true;
            }
        }
    }

    public abstract class Entity
    {
        public string ID;

        private static Dictionary<Type, EntityCollection> _collections = new Dictionary<Type, EntityCollection>();

        private static Thread saveThread = null;

        public static T Create<T>() where T : Entity
        {
            var type = typeof(T);
            var collection = GetCollection(type);
            return (T)collection.CreateObject();
        }

        private static EntityCollection GetCollection(Type type)
        {
            EntityCollection collection;

            if (_collections.ContainsKey(type))
            {
                collection = _collections[type];
            }
            else
            {
                collection = new EntityCollection(type);
                _collections[type] = collection;
            }

            return collection;
        }

        public static T Find<T>(string ID) where T : Entity
        {
            var type = typeof(T);

            var collection = GetCollection(type);
            return (T)_collections[type].FindObject(ID);
        }

        public void Delete()
        {
            var type = this.GetType();
            var collection = GetCollection(type);
            collection.DeleteObject(this.ID);
            RequestBackgroundThread();
        }

        public void Save()
        {
            var type = this.GetType();
            var collection = GetCollection(type);
            collection.RequestSave();            
            RequestBackgroundThread();
        }

        private void RequestBackgroundThread()
        {
            if (saveThread == null)
            {
                saveThread = new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;

                    do
                    {
                        Thread.Sleep(500);
                        foreach (var collection in _collections.Values)
                        {
                            collection.Save();
                        }
                    } while (true);
                });

                saveThread.Start();
            }
        }

        public static IEnumerable<String> GetFields<T>() where T: Entity
        {
            Type type = typeof(T);
            return type.GetFields().Where(f => f.IsPublic).Select(x => x.Name);
        }

        public DataNode Serialize()
        {
            Type type = this.GetType();
            var result = DataNode.CreateObject(type.Name.ToLower());

            var fields = type.GetFields().Where(f => f.IsPublic);
            foreach (var field in fields)
            {
                var val = field.GetValue(this);
                if (val != null)
                {
                    result.AddField(field.Name, val.ToString());
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

                var val = node.GetString(field.Name);

                object obj = val;
                
                field.SetValue(this, obj);                
            }
        }

        public static IEnumerable<T> List<T>() where T:Entity
        {
            var type = typeof(T);
            var result = new List<T>();

            var collection = GetCollection(type);

            foreach (var obj in collection.Objects)
            {
                result.Add((T)obj);
            }
            
            return result;
        }
    }
}
