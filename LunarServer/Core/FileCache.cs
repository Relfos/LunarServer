﻿using LunarLabs.WebServer.HTTP;
using LunarLabs.WebServer.Minifiers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LunarLabs.WebServer.Core
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

        public LoggerCallback logger { get; private set; }

        public FileCache(LoggerCallback logger, string filePath)
        {           
            this.logger = logger;
            this.filePath = filePath + "public/";           
        }

        public HTTPResponse GetFile(HTTPRequest request)
        {
            if (this.filePath == null)
            {
                return null;
            }

            var path = request.path.StartsWith("/") ? request.path.Substring(1): request.path;
            path = filePath + path;
            this.logger(LogLevel.Debug, $"Returning static file...{path}");

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
                    logger(LogLevel.Warning, "Nothing found...");
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

            var ext = Path.GetExtension(path);
            var contentType = GetContentTypeByExtension(ext);

            var result = HTTPResponse.FromBytes(entry.bytes, contentType);

            if (entry.isCompressed)
            {
                result.headers["Content-Encoding"] = "gzip";
            }

            result.headers["Content-Type"] = contentType;

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

        public static string GetContentTypeByExtension(string ext)
        {
            switch (ext)
            {
                case ".avi": return "video/x-msvideo";
                case ".css": return "text/css";
                case ".csv": return "text/csv";
                case ".eot": return "application/vnd.ms-fontobject";
                case ".gz": return "application/gzip";
                case ".gif": return "image/gif";
                case ".html": case ".htm": return "text/html";
                case ".jpeg": case ".jpg": return "image/jpeg";
                case ".js": return "text/javascript";
                case ".json": return "application/json";
                case ".mid": case ".midi": return "audio/midi";
                case ".mp3": return "audio/mpeg";
                case ".mp4": return "video/mp4";
                case ".mpeg": return "video/mpeg";
                case ".oga": case "opus": return "audio/ogg";
                case ".ogv": return "video/ogg";
                case ".png": return "image/png";
                case ".pdf": return "application/pdf";
                case ".txt": return "text/txt";
                case ".wav": return "audio/wav";
                case ".webp": return "image/webp";
                case ".woff": return "font/woff";
                case ".woff2": return "font/woff2";
                case ".wasm": return "application/wasm";
                case ".xml": return "application/xml";
                case ".zip": return "application/zip";
                case ".svg": return "image/svg+xml";
                default: return null;
            }
        }
    }


}
