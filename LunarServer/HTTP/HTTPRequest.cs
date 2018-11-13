using LunarLabs.WebServer.Core;
using System;
using System.Collections.Generic;

namespace LunarLabs.WebServer.HTTP
{
    public struct FileUpload
    {
        public string fileName;
        public string mimeType;
        public byte[] bytes;

        public FileUpload(string fileName, string mimeType, byte[] bytes)
        {
            this.fileName = fileName;
            this.mimeType = mimeType;
            this.bytes = bytes;
        }
    }

    public class HTTPRequest
    {
        public enum Method
        {
            Get,
            Post,
            Head,
            Put,
            Delete
        }

        public HTTPServer server;

        public Method method;
        public string url;
        public string path;
        public string version;

        public byte[] bytes;

        public string postBody;

        public Session session;

        public Dictionary<string, string> headers = new Dictionary<string, string>();
        public Dictionary<string, string> args = new Dictionary<string, string>();

        public List<FileUpload> uploads = new List<FileUpload>();

        public bool HasVariable(string name)
        {
            return args != null && args.ContainsKey(name);
        }

        public string GetVariable(string name)
        {
            if (HasVariable(name))
            {
                return args[name];
            }

            return null;
        }
    }
}
