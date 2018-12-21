using LunarLabs.WebServer.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

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

        public Method method;
        public string url;
        public string path;
        public string version;

        public byte[] bytes;

        public string postBody;

        public Session session;

        public Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
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

        public HTTPResponse Dispatch()
        {
            string host;
            int port;

            if (url.Contains(":"))
            {
                var temp = url.Split(':');
                host = temp[0];
                port = int.Parse(temp[1]);
            }
            else
            {
                host = url;
                port = 80;
            }

            AddDefaultHeader("User-Agent", "User-Agent: Mozilla/4.0 (compatible; MSIE5.01; Windows NT)");
            AddDefaultHeader("Host", host);
            AddDefaultHeader("Accept-Language", "en-us");
            AddDefaultHeader("Accept-Encoding", "gzip");

            var sb = new StringBuilder();

            var version = "HTTP/1.1";
            sb.Append($"{method} {path} {version}\r\n\r\nHost: {host}\r\n");

            foreach (var entry in headers)
            {
                sb.Append($"{entry.Key}: {entry.Value}\r\n");
            }
            sb.Append("\r\n");

            var requestBody = sb.ToString();

            using (var client = new TcpClient(host, port))
            {
                using (NetworkStream nwStream = client.GetStream())
                {
                    byte[] bytesToSend = Encoding.ASCII.GetBytes(requestBody);

                    nwStream.Write(bytesToSend, 0, bytesToSend.Length);

                    using (var reader = new StreamReader(nwStream))
                    {
                        var response = new HTTPResponse();
                        var status = reader.ReadLine();
                        if (status.StartsWith(version))
                        {
                            var code = status.Replace(version, "").TrimStart();
                            response.code = DecodeStatusCode(code);
                        }
                        else
                        {
                            throw new Exception("Invalid HTTP response");
                        }

                        while (true)
                        {
                            var header = reader.ReadLine();
                            if (string.IsNullOrEmpty(header))
                            {
                                break;
                            }

                            var temp = header.Split(':');
                            var key = temp[0].TrimEnd();
                            var val = temp[1].TrimStart();
                            response.headers[key] = val;
                        }
                    }

                }

                client.Close();
            }
        }

        private HTTPCode DecodeStatusCode(string code)
        {
            throw new NotImplementedException();
        }

        private void AddDefaultHeader(string key, string val)
        {
            if (headers.ContainsKey(key))
            {
                return;
            }

            headers[key] = val;
        }
    }
}
