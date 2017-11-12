using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

using SynkServer.Core;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Concurrent;

namespace SynkServer.HTTP
{
    public sealed class HTTPServer: IDisposable
    {
        public Logger log { get; private set; }
        private TcpListener listener;

        public bool running { get; private set; }

        public HTTPServer(Logger log, int port = 80)
        {
            this.log = log;

            var ipAddress = IPAddress.Parse("127.0.0.1");

            RestoreSessionData();

            log.Info("Starting TCP listener...");
            listener = new TcpListener(ipAddress, 80);
        }

        public void Run(Func<HTTPRequest, HTTPResponse> handler)
        {
            listener.Start();

            log.Info("Server is listening on " + listener.LocalEndpoint);

            running = true;
            while (running)
            {

                log.Info("Waiting for a connection...");

                try
                {
                    var client = listener.AcceptSocketAsync().Result;

                    var task = new Task(() => HandleClient(client, handler));
                    task.Start();
                }
                catch (Exception e)
                {
                    log.Error(e.ToString());
                }
            }
        }

        public void Stop()
        {
            running = false;
        }

        private void WriteString(BinaryWriter writer, string s)
        {
            var bytes = System.Text.Encoding.ASCII.GetBytes(s);
            writer.Write(bytes);
            WriteNewLine(writer);
        }

        private void WriteNewLine(BinaryWriter writer)
        {
            writer.Write((byte)13);
            writer.Write((byte)10);
        }

        private void HandleClient(Socket client, Func<HTTPRequest, HTTPResponse> handler)
        {
            log.Info("Connection accepted.");

            try
            {
                List<string> lines;
                byte[] unread;

                if (client.ReadLines(out lines, out unread))
                {
                    var request = new HTTPRequest();

                    var s = lines[0].Split(' ');

                    if (s.Length == 3)
                    {
                        switch (s[0].ToUpperInvariant())
                        {
                            case "GET": request.method = HTTPRequest.Method.Get; break;
                            case "POST": request.method = HTTPRequest.Method.Post; break;
                            case "HEAD": request.method = HTTPRequest.Method.Head; break;
                            case "PUT": request.method = HTTPRequest.Method.Put; break;
                            case "DELETE": request.method = HTTPRequest.Method.Delete; break;

                            default: throw new Exception("Invalid HTTP method: "+s[0]);
                        }
                        
                        request.version = s[2];

                        var path = s[1].Split('?');
                        request.url = path[0];

                        log.Info(request.method.ToString() +" "+ s[1]);

                        if (path.Length > 1)
                        {
                            var temp = path[1].Split('&');
                            foreach (var entry in temp)
                            {
                                var str = entry.Split('=');
                                var key = str[0].UrlDecode();
                                var val = str.Length > 1 ? str[1].UrlDecode() : "";

                                request.args[key] = val;
                            }
                        }

                        for (int i = 1; i < lines.Count; i++)
                        {
                            var temp = lines[i].Split(':');
                            if (temp.Length >= 2)
                            {
                                var key = temp[0];
                                var val = temp[1].TrimStart();

                                request.headers[key] = val;
                            }
                        }

                        if (request.method == HTTPRequest.Method.Post)
                        {
                            if (!ParsePost(request, client, unread))
                            {
                                return;
                            }
                        }
                    }

                    string setCookie;
                    request.session = GetSession(request, out setCookie);

                    var response = handler(request);

                    if (response == null)
                    {
                        response = HTTPResponse.FromString("Not found...", HTTPCode.NotFound);
                    }

                    response.headers["Content-Length"] = response.bytes != null ? response.bytes.Length.ToString() : "0";

                    if (response.code == HTTPCode.OK && response.expiration.TotalSeconds > 0)
                    {
                        response.headers["Date"] = response.date.ToString("r");
                        response.headers["Expires"] = (response.date + response.expiration).ToString("r");
                    }
                    else
                    {
                        response.headers["Cache-Control"] = "no-cache";
                    }

                    if (setCookie != null)
                    {
                        response.headers["Set-Cookie"] = setCookie;
                    }

                    using (var stream = new MemoryStream(1024))
                    {
                        using (var writer = new BinaryWriter(stream))
                        {
                            var answerString = (response.code == HTTPCode.Redirect || response.code == HTTPCode.OK) ? "Found" : "Not Found";
                            var head = "HTTP/1.1 " + (int)response.code + " " + answerString;
                            WriteString(writer, head);
                            foreach (var header in response.headers)
                            {
                                WriteString(writer, header.Key+ ": " + header.Value);
                            }

                            WriteNewLine(writer);

                            writer.Write(response.bytes);
                        }

                        var bytes = stream.ToArray();
                        client.Send(bytes);
                    }
                    
                }


            }
            finally
            {
                client.Dispose();
            }

        }

        private bool ParsePost(HTTPRequest request, Socket client, byte[] unread)
        {
            if (!request.headers.ContainsKey("Content-Length"))
            {
                return true;
            }

            int bodySize;

            var lenStr = request.headers["Content-Length"];

            int.TryParse(lenStr, out bodySize);
            request.bytes = new byte[bodySize];

            if (unread.Length > 0) {
                Array.Copy(unread, request.bytes, unread.Length);
            }

            int ofs = unread.Length;
            int left = bodySize - ofs;

            while (left>0)
            {
                int n = client.Receive(request.bytes, ofs, left, SocketFlags.None);

                if (n <=0)
                {
                    return false;
                }

                ofs += n;
                left -= n;
            }

            var contentTypeHeader = request.headers["Content-Type"];

            if (contentTypeHeader.ToLowerInvariant().StartsWith("multipart/form-data"))
            {
                var parser = new MultipartParser(new MemoryStream(request.bytes), (key, val) =>
                {
                    request.args[key] = val.UrlDecode();
                });

                if (parser.Success)
                {
                    request.uploads.Add(new FileUpload(parser.Filename, parser.ContentType, parser.FileContents));
                }
            }
            else
            {
                var encoding = System.Text.Encoding.UTF8; //request.headers["Content-Encoding"]

                var requestBody = encoding.GetString(request.bytes);

                // TODO: implement multipart/form-data parsing
                // example available here: http://stackoverflow.com/questions/5483851/manually-parse-raw-http-data-with-php


                // verify there is data to parse
                if (String.IsNullOrWhiteSpace(requestBody))
                {
                    return true;
                }

                // define a character for KV pairs
                var kvpSeparator = new[] { '=' };

                // Split the request body into key-value pair strings
                var keyValuePairStrings = requestBody.Split('&');

                foreach (var kvps in keyValuePairStrings)
                {
                    // Skip KVP strings if they are empty
                    if (string.IsNullOrWhiteSpace(kvps))
                        continue;

                    // Split by the equals char into key values.
                    // Some KVPS will have only their key, some will have both key and value
                    // Some other might be repeated which really means an array
                    var kvpsParts = kvps.Split(kvpSeparator, 2);

                    // We don't want empty KVPs
                    if (kvpsParts.Length == 0)
                        continue;

                    // Decode the key and the value. Discard Special Characters
                    var key = WebUtility.UrlDecode(kvpsParts[0]);

                    var value = kvpsParts.Length >= 2 ? System.Net.WebUtility.UrlDecode(kvpsParts[1]) : null;

                    // Simply set the key to the parsed value
                    request.args[key] = value.UrlDecode();
                }
            }

            return true;
        }

        public void Dispose()
        {
            this.Stop();

            if (listener != null)
            {
                listener.Stop();
                listener = null;
            }

            SaveSessionData();
        }

        #region SESSIONS
        private const string SessionCookieName = "_synk_session_";
        private ConcurrentDictionary<string, Session> _sessions = new ConcurrentDictionary<string, Session>(StringComparer.InvariantCultureIgnoreCase);
        public TimeSpan CookieExpiration = TimeSpan.FromMinutes(30);

        private Session GetSession(HTTPRequest request, out string setCookie)
        {
            setCookie = null;

            // expire old sessions
            var allKeys = _sessions.Keys.ToArray();
            foreach (var key in allKeys)
            {
                var sessionInfo = _sessions[key];
                if (DateTime.Now.Subtract(sessionInfo.lastActivity) > this.CookieExpiration)
                {
                    Session temp;
                    _sessions.TryRemove(key, out temp);
                }
            }

            string cookieValue = null;

            var requestCookies = request.headers.ContainsKey("Cookie") ? request.headers["Cookie"] : null;
            if (requestCookies != null)
            {
                string[] cookies = requestCookies.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);

                for (int i=cookies.Length-1; i>=0; i--)
                {
                    var cookie = cookies[i];

                    string[] s = cookie.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    string name = s[0].Trim();
                    string val = s[1];

                    if (name.Equals(SessionCookieName))
                    {
                        cookieValue = val;
                        break;
                    }
                }
            }

            if (cookieValue == null)
            {
                var session = new Session(request);
                cookieValue = session.ID;
                _sessions[cookieValue] = session;

                setCookie = SessionCookieName + "=" + cookieValue;
                log.Info($"Session: {cookieValue}");
                return session;
            }
            if (!_sessions.ContainsKey(cookieValue))
            {
                var session = new Session(request, cookieValue);
                _sessions[cookieValue] = session;

                log.Info($"Session: {cookieValue}");
                return session;
            }
            else
            {
                log.Info($"Session: {cookieValue}");
                var session = _sessions[cookieValue];
                session.lastActivity = DateTime.Now;
                return session;
            }
        }

        private void SaveSessionData()
        {
            foreach (var session in _sessions.Values)
            {
                foreach (var entry in session.data)
                {

                }
            }
        }

        private void RestoreSessionData()
        {
            _sessions.Clear();
        }
        #endregion

    }
}
