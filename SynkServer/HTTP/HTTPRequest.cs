using System;
using System.Collections.Generic;

namespace SynkServer.HTTP
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

        public Method method;
        public string url;
        public string path;
        public string version;

        public byte[] bytes;

        public Dictionary<string, string> headers = new Dictionary<string, string>();
        public Dictionary<string, string> args = new Dictionary<string, string>();

        public List<FileUpload> files = new List<FileUpload>();
    }
}
