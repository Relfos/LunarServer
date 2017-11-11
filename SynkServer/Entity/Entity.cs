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

            var fileName = GetFileName();
            if (File.Exists(fileName))
            {
                var root = XMLReader.ReadFromString(File.ReadAllText(fileName));
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
                    _objects[obj.id] = obj;
                }
            }
        }

        private string GetFileName()
        {
            return "store/"+objectType.Name.ToLower() + ".xml";
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

            obj.id = Guid.NewGuid().ToString();

            lock (this)
            {
                _objects[obj.id] = obj;
            }

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
            lock (this)
            {
                if (_objects.ContainsKey(ID))
                {
                    _objects.Remove(ID);
                }
            }
        }

        public void Save()
        {
            lock (this)
            {
                if (_shouldSave)
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
        public string id;

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

            lock (_collections)
            {
                if (_collections.ContainsKey(type))
                {
                    collection = _collections[type];
                }
                else
                {
                    collection = new EntityCollection(type);
                    _collections[type] = collection;
                }
            }

            return collection;
        }

        public static T FindById<T>(string ID) where T : Entity
        {
            var type = typeof(T);

            var collection = GetCollection(type);
            return (T)collection.FindObject(ID);
        }

        public static T FindOne<T>(Predicate<T> pred) where T : Entity
        {
            var type = typeof(T);

            var collection = GetCollection(type);

            foreach (T item in collection.Objects)
            {
                if (pred(item))
                {
                    return item;
                }
            }

            return default(T);
        }

        public static List<T> FindAll<T>(Predicate<T> pred) where T : Entity
        {
            var type = typeof(T);

            var collection = GetCollection(type);

            var result = new List<T>();

            foreach (T item in collection.Objects)
            {
                if (pred(item))
                {
                    result.Add(item);
                }
            }

            return result;
        }

        public void Delete()
        {
            var type = this.GetType();
            var collection = GetCollection(type);
            collection.DeleteObject(this.id);
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

        public static IEnumerable<T> Every<T>() where T:Entity
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
