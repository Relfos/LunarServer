using SynkServer.HTTP;
using SynkServer.Minifiers;
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
        public bool isDownload;
        public string contentType;

        public void Reload()
        {
            this.bytes = File.ReadAllBytes(path);

            bool shouldCompress;
            this.contentType = MimeUtils.GetContentType(path, out shouldCompress, out isDownload);

            if (!path.Contains(".min."))
            {
                var ext = Path.GetExtension(path);
                switch (ext)
                {
                    case ".css":
                        {
                            var temp = Encoding.UTF8.GetString(bytes);
                            temp = CSSMinifier.Compress(temp);
                            this.bytes = Encoding.UTF8.GetBytes(temp);
                            break;
                        }

                    case ".js":
                        {
                            var temp = Encoding.UTF8.GetString(bytes);
                            temp = JSMinifier.Compress(temp);
                            this.bytes = Encoding.UTF8.GetBytes(temp);
                            break;
                        }

                    case ".html":
                        {
                            var temp = Encoding.UTF8.GetString(bytes);
                            temp = HTMLMinifier.Compress(temp);
                            this.bytes = Encoding.UTF8.GetBytes(temp);
                            break;
                        }
                }
            }

            if (shouldCompress && (this.bytes.Length < 1400 || this.bytes.Length > 1024 * 512))
            {
                shouldCompress = false;
            }

            this.isCompressed = shouldCompress;
            this.hash = StringUtils.MD5(this.bytes);

            if (shouldCompress)
            {
                this.bytes = this.bytes.GZIPCompress();
            }
        }
    }

    public class FileCache
    {
        private const int defaultMaxAge = 2592000;

        protected Dictionary<string, CacheEntry> _files = new Dictionary<string, CacheEntry>();

        public string filePath { get; private set; }

        public Logger log { get; private set; }

        public FileCache(Logger log, string filePath)
        {
            this.log = log;
            this.filePath = filePath;           
        }

        public HTTPResponse GetFile(HTTPRequest request)
        {
            var path = filePath + request.path;
            log.Debug($"Returning static file...{path}");

            CacheEntry entry;

            lock (_files)
            {
                if (_files.ContainsKey(request.path))
                {
                    entry = _files[request.path];
                }
                else
                {
                    entry = null;
                }
            }

            if (entry != null)
            {      
                if (entry.path != null)
                {
                    var lastMod = File.GetLastWriteTime(path);

                    if (lastMod != entry.lastModified)
                    {
                        entry.Reload();
                        entry.lastModified = lastMod;
                    }
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

                lock (_files)
                {
                    _files[request.path] = entry;
                }
            }

            if (request.headers.ContainsKey("If-None-Match"))
            {
                var hash = request.headers["If-None-Match"];
                if (hash.Equals(entry.hash))
                {
                    return HTTPResponse.NotModified(entry.hash, defaultMaxAge);
                }
            }

            var result = HTTPResponse.FromBytes(entry.bytes);

            if (entry.isCompressed)
            {
                result.headers["Content-Encoding"] = "gzip";
            }

            result.headers["Content-Type"] = entry.contentType;

            result.headers["Content-Description"] = "File Transfer";

            var fileName = Path.GetFileName(request.path);

            if (entry.isDownload)
            {
                result.headers["Content-Disposition"] = "attachment; filename=\"" + fileName + "\"";
            }

            result.headers["Content-Transfer-Encoding"] = "binary";
            result.headers["Connection"] = "Keep-Alive";
            //result.headers["Expires"] = "0";
            result.headers["Cache-Control"] = "max-age="+defaultMaxAge + ", public";
            result.headers["Pragma"] = "public";

            
            result.headers["Last-Modified"] = entry.lastModified.ToString("r");

            result.headers["ETag"] = entry.hash;

            return result;
        }
    }


}
