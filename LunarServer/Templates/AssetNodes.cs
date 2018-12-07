using LunarLabs.WebServer.Core;
using System;
using System.Collections.Generic;
using System.IO;

namespace LunarLabs.WebServer.Templates
{
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

            if (this.engine.Server.Settings.Environment == Core.ServerEnvironment.Prod)
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

                var rootPath = this.engine.Server.Cache.filePath + extension + "/";

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
