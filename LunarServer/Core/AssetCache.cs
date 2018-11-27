using LunarLabs.WebServer.Minifiers;
using LunarLabs.WebServer.Templates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LunarLabs.WebServer.Core
{
    public class AssetGroup
    {
        public static readonly string AssetFileName = "lunarpack.";

        public string extension;
        public string contentType;
        public Func<string, string> minifier;

        private int fileCount;

        public string requestPath;

        public AssetGroup(string extension, string contentType, Func<string, string> minifier)
        {
            this.extension = extension;
            this.contentType = contentType;
            this.minifier = minifier;
            this.fileCount = 0;
        }

        public CacheEntry Update(string filePath)
        {
            StringBuilder content;

            lock (AssetNode.assetList)
            {
                if (!AssetNode.assetList.ContainsKey(extension))
                {
                    return null;
                }

                var source = AssetNode.assetList[extension];

                if (source.Count == this.fileCount)
                {
                    return null;
                }

                var path = filePath + extension;

                content = new StringBuilder();

                foreach (var file in source)
                {
                    var srcName = path + "/" + file + "." + extension;
                    if (!File.Exists(srcName))
                    {
                        var minFile = srcName.Replace("." + extension, ".min." + extension);
                        if (File.Exists(minFile))
                        {
                            srcName = minFile;
                        }
                        else
                        {
                            throw new Exception("Could not find: " + srcName);
                        }
                    }
                    var temp = File.ReadAllText(srcName);

                    if (minifier != null)
                    {
                        temp = minifier(temp);
                    }

                    content.Append(temp);
                    content.AppendLine();
                }

                this.fileCount = source.Count;
            }

            var bytes = Encoding.UTF8.GetBytes(content.ToString());

            var fileName = AssetFileName + extension;

            var entry = new CacheEntry();
            entry.hash = StringUtils.MD5(bytes);
            entry.lastModified = DateTime.UtcNow;
            entry.path = null;
            entry.contentType = contentType;

            entry.isCompressed = true;
            entry.bytes = bytes.GZIPCompress();

            this.requestPath = "/" + extension + "/" + fileName;

            return entry;
        }
    }

    public class AssetCache : FileCache
    {
        private List<AssetGroup> groups = new List<AssetGroup>();

        public AssetCache(Site site, string filePath) : base(site.Logger, filePath)
        {
            BuildAssetCache("js", "application/javascript", x => JSMinifier.Compress(x));
            BuildAssetCache("css", "text/css", x => CSSMinifier.Compress(x));
        }

        private void BuildAssetCache(string filter, string contentType, Func<string, string> minifier = null)
        {
            groups.Add(new AssetGroup(filter, contentType, minifier));
        }

        public void Update()
        {
            lock (groups)
            {
                foreach (var group in groups)
                {
                    var entry = group.Update(this.filePath);
                    if (entry != null)
                    {
                        lock (_files)
                        {
                            _files[group.requestPath] = entry;
                        }
                    }
                }
            }
        }
    }
}
