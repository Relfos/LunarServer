using SynkServer.HTTP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SynkServer.Core
{
    public class CacheEntry
    {
        public string path;
        public DateTime lastModified;
        public string hash;
        public byte[] bytes;
        public bool isCompressed;
        public string contentType;

        public void Reload()
        {
            this.bytes = File.ReadAllBytes(path);

            bool shouldCompress;
            this.contentType = MimeUtils.GetContentType(path, out shouldCompress);

            if (shouldCompress && (this.bytes.Length < 1400 || this.bytes.Length < 1024 * 128))
            {
                shouldCompress = false;
            }

            this.isCompressed = shouldCompress;
            this.hash = Utils.MD5(this.bytes);

            if (shouldCompress)
            {
                this.bytes = this.bytes.GZIPCompress();
            }
        }
    }

    public class Cache
    {
        private Dictionary<string, CacheEntry> _files = new Dictionary<string, CacheEntry>();

        private string rootPath;

        public Logger log { get; private set; }

        public Cache(Logger log, string filePath)
        {
            this.log = log;
            this.rootPath = filePath;
        }

        public HTTPResponse GetFile(HTTPRequest request)
        {
            var path = rootPath + request.url;
            log.Debug($"Returning static file...{path}");

            CacheEntry entry;

            if (_files.ContainsKey(request.url))
            {
                entry = _files[request.url];                

                var lastMod = File.GetLastWriteTime(path);

                if (lastMod != entry.lastModified)
                {
                    entry.Reload();
                    entry.lastModified = lastMod;
                }
            }
            else
            {
                if (!File.Exists(path))
                {
                    log.Warning("Nothing found...");
                    return null;
                }

                entry = new CacheEntry();
                entry.lastModified = File.GetLastWriteTime(path);
                entry.path = path;

                entry.Reload();

                _files[request.url] = entry;
            }

            if (request.headers.ContainsKey("If-None-Match"))
            {
                var hash = request.headers["If-None-Match"];
                if (hash.Equals(entry.hash))
                {
                    return HTTPResponse.NotModified();
                }
            }

            var result = HTTPResponse.FromBytes(entry.bytes);

            if (entry.isCompressed)
            {
                result.headers["Content-Encoding"] = "gzip";
            }

            result.headers["Content-Type"] = entry.contentType;

            result.headers["Content-Description"] = "File Transfer";

            var fileName = Path.GetFileName(request.url);

            result.headers["Content-Disposition"] = "attachment; filename=\"" + fileName + "\"";
            result.headers["Content-Transfer-Encoding"] = "binary";
            result.headers["Connection"] = "Keep-Alive";
            //result.headers["Expires"] = "0";
            result.headers["Cache-Control"] = "max-age=120";
            result.headers["Pragma"] = "public";

            
            result.headers["Last-Modified"] = entry.lastModified.ToString("r");

            result.headers["ETag"] = entry.hash;

            return result;
        }
    }
}
