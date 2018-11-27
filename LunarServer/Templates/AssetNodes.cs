using LunarLabs.Parser;
using LunarLabs.Parser.XML;
using LunarLabs.Parser.JSON;
using LunarLabs.WebServer.Core;
using System;
using System.Collections.Generic;
using System.IO;
using LunarLabs.Parser.CSV;
using LunarLabs.Parser.YAML;

namespace LunarLabs.WebServer.Templates
{
    public class StoreNode : TemplateNode
    {
        public static Dictionary<string, DataNode> _storeMap = new Dictionary<string, DataNode>();
        private string _key;

        private static readonly string[] _extensions = new string[] { ".xml", ".json", ".csv", ".yaml" };

        public StoreNode(TemplateDocument document, string key) : base(document)
        {
            this._key = key;
        }

        public override void Execute(RenderingContext context)
        {
            DataNode store;

            if (_storeMap.ContainsKey(_key))
            {
                store = _storeMap[_key];
            }
            else
            {
                var basePath = this.engine.Server.Settings.path + "store";

                string fileName = null;
                string targetExtension = null;

                foreach (var extension in _extensions)
                {
                    var temp = basePath + "/" + _key + extension;
                    if (File.Exists(temp))
                    {
                        targetExtension = extension;
                        fileName = temp;
                        break;
                    }
                }

                if (fileName == null)
                {
                    throw new TemplateException("Could not find any file for store: " + _key);
                }

                var contents = File.ReadAllText(fileName);

                switch (targetExtension)
                {
                    case ".xml": store = XMLReader.ReadFromString(contents); break;
                    case ".json": store = JSONReader.ReadFromString(contents); break;
                    case ".csv": store = CSVReader.ReadFromString(contents); break;
                    case ".yaml": store = YAMLReader.ReadFromString(contents); break;
                    default: throw new TemplateException("Unsupported store extension: " + targetExtension);
                }

                if (store.Name == null)
                {
                    store = store.GetNodeByIndex(0);
                }

                _storeMap[_key] = store;
            }

            context.Set(_key, store);
        }
    }

    public class AssetNode : TemplateNode
    {
        public static Dictionary<string, List<string>> assetList = new Dictionary<string, List<string>>();

        private string key;
        private string extension;

        private bool skip;

        public AssetNode(TemplateDocument document, string key, string extension) : base(document)
        {
            this.key = key;
            this.extension = extension;

            this.skip = false;

            bool found = false;

            List<string> list;

            if (assetList.ContainsKey(extension))
            {
                list = assetList[extension];

                foreach (var entry in list)
                {
                    if (entry == key)
                    {
                        found = true;
                        break;
                    }
                }
            }
            else
            {
                list = new List<string>();
                assetList[extension] = list;
            }

            if (!found)
            {
                list.Add(key);
            }

            foreach (var node in document.Nodes)
            {
                if (node == this)
                {
                    break;
                }

                var an = node as AssetNode;
                if (an != null && an.extension == this.extension)
                {
                    skip = true;
                    break;
                }
            }
        }

        public override void Execute(RenderingContext context)
        {
            string fileName;

            if (this.engine.Server.Settings.environment == Core.ServerEnvironment.Prod)
            {
                if (skip)
                {
                    return;
                }

                fileName = AssetGroup.AssetFileName + extension;
            }
            else
            {
                fileName = key + "." + extension;

                var rootPath = this.engine.Site.Cache.filePath + extension + "/";

                if (!File.Exists(rootPath + fileName))
                {
                    var minFile = fileName.Replace("."+extension, ".min."+ extension);
                    if (File.Exists(rootPath + minFile))
                    {
                        fileName = minFile;
                    }
                    else
                    {
                        throw new Exception("Could not find asset: " + fileName);
                    }
                }
            }

            string html;

            switch (extension)
            {
                case "js": html = "<script src=\"/js/" + fileName + "\"></script>"; break;
                case "css": html = "<link href=\"/css/"+fileName+"\" rel=\"stylesheet\">"; break;
                default: throw new Exception("Unsupport asset extension: " + extension);
            }

            context.output.Append(html);
        }
    }

}
