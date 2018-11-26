using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading.Tasks;
using LunarLabs.WebServer.Core;

namespace LunarLabs.WebServer.HTTP
{
    public sealed class HTTPServer : IDisposable
    {
        private Socket listener;

        public Logger Logger { get; private set; }

        public bool AutoCompress = true;

        public bool Running { get; private set; }

        public DateTime StartTime { get; private set; }

        public Action<HTTPRequest> OnNewVisitor;

        public ServerSettings Settings { get; private set; }

        public SessionStorage SessionStorage { get; private set; }

        public Site Site { get; set; }

        public HTTPServer(ServerSettings settings, Logger log = null, SessionStorage sessionStorage = null)
        {
            this.SessionStorage = sessionStorage != null ? sessionStorage : new MemorySessionStorage();
            this.Logger = log != null ? log : new NullLogger();
            this.StartTime = DateTime.Now;

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

            this.Settings = settings;

            SessionStorage.Restore();

            var fullPath = settings.path;

            log.Info($"~LUNAR SERVER~ [{settings.environment} mode]");
            log.Info($"Port: {settings.port}");

            if (fullPath != null)
            {
                fullPath = fullPath.Replace("\\", "/");
                log.Info($"Root path: {fullPath}");
            }
            else
            {
                log.Info($"No root path specified.");
            }

            // Create a TCP/IP socket
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Blocking = true;
            listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
        }

        public void Run()
        {
            if (Site == null)
            {
                Logger.Error("Site required, can't be null");
                return;
            }

            Logger.Info("Initializating site: " + Site.Host);
            Site.Initialize();

            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, Settings.port);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                Logger.Error(e.StackTrace);
                return;
            }

            // log.Info("Server is listening on " + listener.LocalEndpoint);

            Running = true;

            while (Running)
            {

                Logger.Debug("Waiting for a connection...");

                try
                {
                    var client = listener.Accept();

                    Logger.Debug("Got a connection...");

                    Task.Run(() => { HandleClient(client); });
                }
                catch (Exception e)
                {
                    Logger.Error(e.ToString());
                }
            }
        }

        public void Stop()
        {
            Running = false;
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
            Logger.Debug("Connection accepted.");

            try
            {
                // Disable the Nagle Algorithm for this tcp socket.
                client.NoDelay = true;

                // Set the receive buffer size to 8k
                client.ReceiveBufferSize = 8192;

                // Set the timeout for synchronous receive methods
                client.ReceiveTimeout = 5000;

                // Set the send buffer size to 8k.
                client.SendBufferSize = 8192;

                // Set the timeout for synchronous send methods
                client.SendTimeout = 5000;

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
                        Logger.Debug(line);
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

                        Logger.Info(request.method.ToString() + " " + s[1]);

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
                                Logger.Error("Failed parsing post data");
                                return;
                            }
                        }
                    }
                    else
                    {
                        Logger.Error("Failed parsing request method");
                        return;
                    }

                    string setCookie;
                    request.session = GetSession(request, out setCookie);

                    if (setCookie != null && OnNewVisitor != null)
                    {
                        Logger.Debug("Handling visitors...");
                        OnNewVisitor(request);
                    }

                    Logger.Debug("Handling request...");

                    HTTPResponse response = Site.HandleRequest(request);

                    if (response == null || response.bytes == null)
                    {
                        Logger.Debug($"Got no response...");
                        response = HTTPResponse.FromString("Not found...", HTTPCode.NotFound);
                    }
                    else
                    {
                        Logger.Debug($"Got response with {response.bytes.Length} bytes...");
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
                    Logger.Error("Failed parsing request data");
                }


            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
                // ignore
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

            SessionStorage.Save();
        }

        #region SESSIONS
        private const string SessionCookieName = "_lunar_session_";

        private Session GetSession(HTTPRequest request, out string setCookie)
        {
            setCookie = null;

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

            Session session;
            SessionStorage.Update();

            if (cookieValue == null)
            {
                session = SessionStorage.CreateSession();
                cookieValue = session.ID;

                setCookie = SessionCookieName + "=" + cookieValue;
                Logger.Debug($"Session: {cookieValue}");
            }
            else
            if (SessionStorage.HasSession(cookieValue))
            {
                Logger.Debug($"Session: {cookieValue}");
                session = SessionStorage.GetSession(cookieValue);
                session.lastActivity = DateTime.Now;
                return session;
            }
            else
            {
                session = SessionStorage.CreateSession(cookieValue);
                setCookie = SessionCookieName + "=" + cookieValue;
                Logger.Debug($"Session: {cookieValue}");
                return session;
            }

            return session;
        }

        #endregion

    }
}
