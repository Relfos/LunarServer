using LunarLabs.Parser;
using LunarLabs.Parser.CSV;
using LunarLabs.Parser.JSON;
using LunarLabs.Parser.XML;
using LunarLabs.WebServer.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace LunarLabs.WebServer.Entity
{
    public class EntityStore
    {
        private Type CollectionType;
        private Dictionary<Type, EntityConnector> _collections = new Dictionary<Type, EntityConnector>();

        private Thread saveThread = null;
        internal ServerSettings settings;

        public EntityStore(ServerSettings settings, Type collectionType)
        {
            this.settings = settings;
            this.CollectionType = collectionType;

            if (collectionType.BaseType.IsAssignableFrom(typeof(EntityConnector)))
            {
                throw new ArgumentException("Collection type must be valid");
            }
        }

        public static EntityStore Create<T>(ServerSettings settings) where T : EntityConnector
        {
            return new EntityStore(settings, typeof(T));
        }

        public T Create<T>() where T : Entity
        {
            var type = typeof(T);
            var collection = GetCollection(type);
            return (T)collection.CreateObject();
        }

        internal void RequestBackgroundThread()
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

        internal EntityConnector GetCollection(Type type)
        {
            EntityConnector collection;

            lock (_collections)
            {
                if (_collections.ContainsKey(type))
                {
                    collection = _collections[type];
                }
                else
                {
                    collection = (EntityConnector)Activator.CreateInstance(CollectionType);
                    collection.Initialize(this, type);
                    _collections[type] = collection;
                }
            }

            return collection;
        }

        public T FindById<T>(string ID) where T : Entity
        {
            var type = typeof(T);

            var collection = GetCollection(type);
            return (T)collection.FindObject(ID);
        }

        public T FindOne<T>(Predicate<T> pred) where T : Entity
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

        public List<T> FindAll<T>(Predicate<T> pred) where T : Entity
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

        public IEnumerable<T> Every<T>() where T : Entity
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


    public abstract class LunarStore : EntityConnector 
    {
        public abstract DataNode ReadStore(string content);
        public abstract string SaveStore(DataNode root);

        internal override void LoadConnector()
        {
            var fileName = GetFileName();
            if (File.Exists(fileName))
            {
                var contents = File.ReadAllText(fileName);
                var root = ReadStore(contents);
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
                    AddObject(obj);
                }
            }
        }

        internal override void SaveConnector()
        {
            var result = DataNode.CreateObject("collection");

            foreach (var obj in this.Objects)
            {
                var node = obj.Serialize();
                result.AddNode(node);
            }

            var contents = SaveStore(result);
            File.WriteAllText(GetFileName(), contents);
        }

        private string GetFileName()
        {
            return Store.settings.Path + "store/" + objectType.Name.ToLower() + ".xml";
        }
    }

    public class XMLStore : LunarStore
    {
        public override DataNode ReadStore(string content)
        {
            return XMLReader.ReadFromString(content);
        }

        public override string SaveStore(DataNode root)
        {
            return XMLWriter.WriteToString(root);
        }
    }

    public class JSONStore : LunarStore
    {
        public override DataNode ReadStore(string content)
        {
            return JSONReader.ReadFromString(content);
        }

        public override string SaveStore(DataNode root)
        {
            return JSONWriter.WriteToString(root);
        }
    }

    public class CSVStore : LunarStore
    {
        public override DataNode ReadStore(string content)
        {
            return CSVReader.ReadFromString(content);
        }

        public override string SaveStore(DataNode root)
        {
            return CSVWriter.WriteToString(root);
        }
    }

}
