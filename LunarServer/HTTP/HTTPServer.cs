using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Concurrent;
using LunarLabs.WebServer.Core;

namespace LunarLabs.WebServer.HTTP
{

    public sealed class HTTPServer : IDisposable
    {
        public Logger log { get; private set; }
        private Socket listener;

        public bool running { get; private set; }

        public DateTime startTime { get; private set; }

        public Action<HTTPRequest> OnNewVisitor;

        public ServerSettings settings { get; private set; }

        public Site Site { get; private set; }

        public HTTPServer(Logger log, ServerSettings settings)
        {
            this.log = log;
            this.startTime = DateTime.Now;

            if (log.level == LogLevel.Default)
            {
                if (settings.environment == ServerEnvironment.Prod)
                {
                    log.level = LogLevel.Info;
                }
                else
                {
                    log.level = LogLevel.Debug;
                }
            }

            this.settings = settings;

            RestoreSessionData();

            var fullPath = settings.path;
            fullPath = fullPath.Replace("\\", "/");
            log.Info($"~LUNAR SERVER~ [{settings.environment} mode]");
            log.Info($"Port: {settings.port}");
            log.Info($"Root path: {fullPath}");

            // Create a TCP/IP socket
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Blocking = true;
            listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
        }

        public void Run(Site site)
        {
            this.Site = site;
            log.Info("Initializating site: " + Site.host);
            Site.Initialize();

            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, settings.port);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

            }
            catch (Exception e)
            {
                log.Error(e.Message);
                log.Error(e.StackTrace);
                return;
            }

            // log.Info("Server is listening on " + listener.LocalEndpoint);

            running = true;

            while (running)
            {

                log.Debug("Waiting for a connection...");

                try
                {
                    var client = listener.Accept();

                    log.Debug("Got a connection...");

                    Task.Run(() => { HandleClient(client); });
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

        private void HandleClient(Socket client)
        {
            log.Debug("Connection accepted.");

            try
            {
                // Disable the Nagle Algorithm for this tcp socket.
                client.NoDelay = true;

                // Set the receive buffer size to 8k
                client.ReceiveBufferSize = 8192;

                // Set the timeout for synchronous receive methods to 
                // 1 second (1000 milliseconds.)
                client.ReceiveTimeout = 1000;

                // Set the send buffer size to 8k.
                client.SendBufferSize = 8192;

                // Set the timeout for synchronous send methods
                // to 1 second (1000 milliseconds.)			
                client.SendTimeout = 1000;

                // Set the Time To Live (TTL) to 42 router hops.
                client.Ttl = 42;

                List<string> lines;
                byte[] unread;

                if (client.ReadLines(out lines, out unread))
                {
                    var request = new HTTPRequest();
                    request.server = this;

                    foreach (var line in lines)
                    {
                        log.Debug(line);
                    }

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

                            default: throw new Exception("Invalid HTTP method: " + s[0]);
                        }

                        request.version = s[2];

                        var path = s[1].Split('?');
                        request.path = path[0];
                        request.url = s[1];

                        log.Info(request.method.ToString() + " " + s[1]);

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
                                log.Error("Failed parsing post data");
                                return;
                            }
                        }
                    }
                    else
                    {
                        log.Error("Failed parsing request method");
                        return;
                    }

                    string setCookie;
                    request.session = GetSession(request, out setCookie);

                    if (setCookie != null && OnNewVisitor != null)
                    {
                        log.Debug("Handling visitors...");
                        OnNewVisitor(request);
                    }

                    log.Debug("Handling request...");

                    HTTPResponse response = Site.HandleRequest(request);

                    if (response == null || response.bytes == null)
                    {
                        log.Debug($"Got no response...");
                        response = HTTPResponse.FromString("Not found...", HTTPCode.NotFound);
                    }
                    else
                    {
                        log.Debug($"Got response with {response.bytes.Length} bytes...");
                    }

                    response.headers["Content-Length"] = response.bytes != null ? response.bytes.Length.ToString() : "0";

                    if (response.code == HTTPCode.OK && response.expiration.TotalSeconds > 0)
                    {
                        response.headers["Date"] = response.date.ToString("r");
                        response.headers["Expires"] = (response.date + response.expiration).ToString("r");
                    }
                    else
                    if (!response.headers.ContainsKey("Cache-Control"))
                    {
                        response.headers["Cache-Control"] = "no-cache";
                    }

                    if (setCookie != null)
                    {
                        response.headers["Set-Cookie"] = setCookie;
                    }

                    using (var stream = new NetworkStream(client))
                    {
                        using (var writer = new BinaryWriter(stream))
                        {
                            var answerString = (response.code == HTTPCode.Redirect || response.code == HTTPCode.OK) ? "Found" : "Not Found";
                            var head = "HTTP/1.1 " + (int)response.code + " " + answerString;
                            WriteString(writer, head);
                            foreach (var header in response.headers)
                            {
                                WriteString(writer, header.Key + ": " + header.Value);
                            }

                            WriteNewLine(writer);

                            writer.Write(response.bytes);
                        }
                    }

                }
                else
                {
                    log.Error("Failed parsing request data");
                }


            }
            finally
            {
                client.Close();
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

            if (unread.Length > 0)
            {
                Array.Copy(unread, request.bytes, unread.Length);
            }

            int ofs = unread.Length;
            int left = bodySize - ofs;

            while (left > 0)
            {
                int n = client.Receive(request.bytes, ofs, left, SocketFlags.None);

                if (n <= 0)
                {
                    return false;
                }

                ofs += n;
                left -= n;
            }

            var contentTypeHeader = request.headers.ContainsKey("Content-Type") ? request.headers["Content-Type"] : "application/x-www-form-urlencoded; charset=UTF-8";

            contentTypeHeader = contentTypeHeader.ToLowerInvariant();

            if (contentTypeHeader.StartsWith("multipart/form-data"))
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
            if (contentTypeHeader.StartsWith("application/x-www-form-urlencoded"))
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
            else
            {
                var encoding = System.Text.Encoding.UTF8; //request.headers["Content-Encoding"]

                request.postBody = encoding.GetString(request.bytes);
            }

            return true;
        }

        public void Dispose()
        {
            this.Stop();

            if (listener != null)
            {
                listener.Close();
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

                for (int i = cookies.Length - 1; i >= 0; i--)
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
                log.Debug($"Session: {cookieValue}");
                return session;
            }

            if (!_sessions.ContainsKey(cookieValue))
            {
                var session = new Session(request, cookieValue);
                _sessions[cookieValue] = session;

                setCookie = SessionCookieName + "=" + cookieValue;
                log.Debug($"Session: {cookieValue}");
                return session;
            }
            else
            {
                log.Debug($"Session: {cookieValue}");
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
