using System;
using System.IO;
using System.Collections.Generic;
using LunarLabs.Parser;
using LunarLabs.Parser.XML;
using LunarLabs.Parser.JSON;
using LunarLabs.Parser.CSV;
using LunarLabs.Parser.YAML;

namespace LunarLabs.WebServer.Templates
{
    public class StoreEntry
    {
        private string path;
        private string targetExtension;

        public DateTime lastTime { get; private set; }
        public DataNode content { get; private set; }

        public StoreEntry(string path)
        {
            this.path = path;
            this.targetExtension = Path.GetExtension(path);
            this.lastTime = DateTime.MinValue;
        }

        internal void Reload()
        {
            var diff = DateTime.UtcNow - lastTime;

            if (diff.TotalSeconds < 10)
            {
                return;
            }

            var lastWriteTime = File.GetLastWriteTime(path);
            if (lastWriteTime < lastTime)
            {
                return;
            }

            var contents = File.ReadAllText(path);

            switch (targetExtension)
            {
                case ".xml": content = XMLReader.ReadFromString(contents); break;
                case ".json": content = JSONReader.ReadFromString(contents); break;
                case ".csv": content = CSVReader.ReadFromString(contents); break;
                case ".yaml": content = YAMLReader.ReadFromString(contents); break;
                default: throw new TemplateException("Unsupported store extension: " + targetExtension);
            }

            if (content.Name == null)
            {
                content = content.GetNodeByIndex(0);
            }

            lastTime = DateTime.UtcNow;
        }
    }

    public class StoreNode : TemplateNode
    {
        public static Dictionary<string, StoreEntry> _storeMap = new Dictionary<string, StoreEntry>();
        private string _key;

        private static readonly string[] _extensions = new string[] { ".xml", ".json", ".csv", ".yaml" };

        public StoreNode(TemplateDocument document, string key) : base(document)
        {
            this._key = key;
        }

        private DataNode FetchStore()
        {
            StoreEntry entry;

            if (_storeMap.ContainsKey(_key))
            {
                entry = _storeMap[_key];
                entry.Reload();

                return entry.content;
            }

            var basePath = this.engine.Server.Settings.Path + "store";

            string fileName = null;
            
            foreach (var extension in _extensions)
            {
                var temp = basePath + "/" + _key + extension;
                if (File.Exists(temp))
                {
                    fileName = temp;
                    break;
                }
            }

            if (fileName == null)
            {
                throw new TemplateException("Could not find any file for store: " + _key);
            }

            entry = new StoreEntry(fileName);
            
            _storeMap[_key] = entry;

            entry.Reload();
            return entry.content;
        }

        public override void Execute(RenderingContext context)
        {
            var store = FetchStore();
            context.Set(_key, store);
        }
    }

}
