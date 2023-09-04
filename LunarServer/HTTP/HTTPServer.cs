using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading.Tasks;
using LunarLabs.WebServer.Core;
using LunarLabs.Parser;
using LunarLabs.Parser.JSON;
using System.Text;
using System.Security.Cryptography;
using System.Linq;
using LunarLabs.WebSockets;
using System.Threading;

namespace LunarLabs.WebServer.HTTP
{
    public class NullByteInjectionException : Exception
    {
        public NullByteInjectionException() : base ("NullByte Injection detected")
        {

        }
    }

    public class HTTPServer : IDisposable
    {
        private Socket listener;
        private AssetCache _assetCache;
        private RequestCache _requestCache = new RequestCache();
        private Router _router;
        private List<ServerPlugin> _plugins = new List<ServerPlugin>();

        private Dictionary<string, Action<WebSocket>> _websocketsHandlers = new Dictionary<string, Action<WebSocket>>();
        private List<WebSocket> _activeWebsockets = new List<WebSocket>();
        private readonly Func<MemoryStream> _bufferFactory;
        private readonly BufferPool _bufferPool;

        public FileCache Cache => _assetCache;
        public IEnumerable<ServerPlugin> Plugins { get { return _plugins; } }
        public LoggerCallback Logger { get; private set; }

        public bool Running { get; private set; }

        public DateTime StartTime { get; private set; }

        public Action<HTTPRequest> OnNewVisitor;
        public Func<HTTPRequest, HTTPResponse> OnNotFound;
        public Func<Exception, HTTPResponse> OnException;

        public ServerSettings Settings { get; private set; }

        public SessionStorage SessionStorage { get; private set; }

        public HTTPServer(ServerSettings settings, LoggerCallback log = null, SessionStorage sessionStorage = null)
        {
            this.SessionStorage = sessionStorage != null ? sessionStorage : new MemorySessionStorage();
            this.Logger = log != null ? log : (_, __) => {};
            this.StartTime = DateTime.Now;

            _bufferPool = new BufferPool();
            _bufferFactory = _bufferPool.GetBuffer;

            this._router = new Router();

            this.Settings = settings;

            SessionStorage.Restore();

            var fullPath = settings.Path;

            this.OnNotFound = (request) =>
            {
                return HTTPResponse.FromString("Not found...", HTTPCode.NotFound);
            };

            this.OnException = (exception) =>
            {
                return HTTPResponse.FromString("Exception: " + exception.Message, HTTPCode.InternalServerError);
            };

            Logger(LogLevel.Info, $"~LUNAR SERVER~ [{settings.Environment} mode] using port: {settings.Port}");

            if (fullPath != null)
            {
                fullPath = fullPath.Replace("\\", "/");
                if (!fullPath.EndsWith("/"))
                {
                    fullPath += "/";
                }

                Logger(LogLevel.Info, $"Root path: {fullPath}");

                this._assetCache = new AssetCache(Logger, fullPath);
            }
            else
            {
                Logger(LogLevel.Warning, $"No root path specified.");

                this._assetCache = null;
            }

            // Create a TCP/IP socket
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Blocking = true;
            listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
        }

        public void Run()
        {
            foreach (var plugin in Plugins)
            {
                Logger(LogLevel.Info, "Initializating plugin: " + plugin.GetType().Name);
                plugin.Install();
            }

            IPAddress bindIP;

            if (string.IsNullOrEmpty(Settings.BindingHost))
            {
                bindIP = IPAddress.Any;
            }
            else
            {
                bindIP = IPAddress.Parse(Settings.BindingHost);
                Logger(LogLevel.Warning, "Will only accept connections coming from " + Settings.BindingHost);
            }

            IPEndPoint localEndPoint = new IPEndPoint(bindIP, Settings.Port);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(1000);

            }
            catch (Exception e)
            {
                Logger(LogLevel.Error, e.ToString());
                return;
            }

            // log.Info("Server is listening on " + listener.LocalEndpoint);

            Running = true;

            while (Running)
            {

                Logger(LogLevel.Debug, "Waiting for a connection...");

                try
                {
                    var client = listener.Accept();

                    Logger(LogLevel.Debug, "Got a connection...");

                    Task.Run(() => { HandleClient(client); });
                }
                catch (Exception e)
                {
                    Logger(LogLevel.Error, e.ToString());
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

        private bool _pingPong;

        private void PingPong()
        {
            if (_pingPong)
            {
                return;
            }

            _pingPong = true;

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                while (Running)
                {
                    lock (_activeWebsockets)
                    {
                        foreach (var socket in _activeWebsockets)
                        {
                            if (socket.State == WebSocketState.Open && socket.NeedsPing)
                            {
                                var diff = DateTime.UtcNow - socket.LastPingPong;
                                if (diff.TotalMilliseconds >= socket.KeepAliveInterval)
                                {
                                    socket.SendPing();
                                }
                            }
                        }
                    }
                    Thread.Sleep(100);
                }
            }).Start();

        }


        private void HandleClient(Socket client)
        {
            Logger(LogLevel.Debug, "Connection accepted.");

            try
            {
                // Disable the Nagle Algorithm for this tcp socket.
                client.NoDelay = true;

                // Set the receive buffer size to 8k
                client.ReceiveBufferSize = 8192;

                // Set the timeout for synchronous receive methods
                //client.ReceiveTimeout = 5000;

                // Set the send buffer size to 8k.
                client.SendBufferSize = 8192;

                // Set the timeout for synchronous send methods
                //client.SendTimeout = 5000;

                // Set the Time To Live (TTL) to 42 router hops.
                client.Ttl = 42;

                bool keepAlive = false;

                int requestCount = 0;

                using (var stream = new NetworkStream(client))
                {
                    using (var reader = new BinaryReader(stream))
                    {
                        using (var writer = new BinaryWriter(stream))
                        {
                            {
                                do
                                {
                                    var lines = new List<string>();
                                    HTTPRequest request = null;

                                    var line = new StringBuilder();
                                    char prevChar;
                                    char currentChar = '\0';
                                    while (true)
                                    {
                                        prevChar = currentChar;
                                        currentChar = (char)reader.ReadByte();

                                        if (currentChar == '\n' && prevChar == '\r')
                                        {
                                            if (line.Length == 0)
                                            {
                                                request = new HTTPRequest();
                                                break;
                                            }

                                            var temp = line.ToString();
                                            Logger(LogLevel.Debug, temp);

                                            if (temp.Contains("\0"))
                                            {
                                                throw new NullByteInjectionException();
                                            }

                                            lines.Add(temp);
                                            line.Length = 0;
                                        }
                                        else
                                        if (currentChar != '\r' && currentChar != '\n')
                                        {
                                            line.Append(currentChar);
                                        }
                                    }

                                    bool isWebSocket = false;

                                    // parse headers
                                    if (request != null)
                                    {
                                        for (int i = 1; i < lines.Count; i++)
                                        {
                                            var temp = lines[i].Split(':');
                                            if (temp.Length >= 2)
                                            {
                                                var key = temp[0];
                                                var val = lines[i].Substring(key.Length + 1).TrimStart();

                                                request.headers[key] = val;

                                                if (key.Equals("Content-Length", StringComparison.InvariantCultureIgnoreCase))
                                                {
                                                    int contentLength = int.Parse(val);
                                                    request.bytes = reader.ReadBytes(contentLength);
                                                }
                                                else
                                                if (key.Equals("Upgrade", StringComparison.InvariantCultureIgnoreCase))
                                                {
                                                    isWebSocket = true;
                                                }
                                            }
                                        }
                                    }

                                    if (request != null)
                                    {
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
                                                case "OPTIONS": request.method = HTTPRequest.Method.Options; break;

                                                default: throw new Exception("Invalid HTTP method: " + s[0]);
                                            }

                                            request.version = s[2];

                                            var path = s[1].Split('?');
                                            request.path = path[0];
                                            request.url = s[1];

                                            Logger(LogLevel.Info, request.method.ToString() + " " + s[1]);

                                            if (isWebSocket)
                                            {
                                                Action<WebSocket> handler = null;
                                                string targetProtocol = null;

                                                var protocolHeader = "Sec-WebSocket-Protocol";

                                                if (request.headers.ContainsKey(protocolHeader))
                                                {
                                                    var protocols = request.headers[protocolHeader].Split(',').Select(x => x.Trim());
                                                    foreach (var protocol in protocols)
                                                    {
                                                        var key = MakeWebSocketKeyPair(protocol, request.path);
                                                        if (_websocketsHandlers.ContainsKey(key))
                                                        {
                                                            targetProtocol = protocol;
                                                            handler = _websocketsHandlers[key];
                                                            break;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    targetProtocol = null;
                                                    var key = MakeWebSocketKeyPair(targetProtocol, request.path);
                                                    if (_websocketsHandlers.ContainsKey(key))
                                                    {
                                                        handler = _websocketsHandlers[key];
                                                    }
                                                }

                                                if (handler != null)
                                                {
                                                    var key = request.headers["Sec-WebSocket-Key"];
                                                    key = GenerateWebSocketKey(key);

                                                    var sb = new StringBuilder();
                                                    sb.Append("HTTP/1.1 101 Switching Protocols\r\n");
                                                    sb.Append("Upgrade: websocket\r\n");
                                                    sb.Append("Connection: Upgrade\r\n");
                                                    sb.Append($"Sec-WebSocket-Accept: {key}\r\n");
                                                    if (targetProtocol != null)
                                                    {
                                                        sb.Append($"Sec-WebSocket-Protocol: {targetProtocol}\r\n");
                                                    }
                                                    sb.Append("\r\n");

                                                    var bytes = Encoding.ASCII.GetBytes(sb.ToString());
                                                    writer.Write(bytes);

                                                    string secWebSocketExtensions = null;
                                                    var keepAliveInterval = 5000;
                                                    var includeExceptionInCloseResponse = true;
                                                    var webSocket = new WebSocket(_bufferFactory, stream, keepAliveInterval, secWebSocketExtensions, includeExceptionInCloseResponse, false, targetProtocol, Settings.MaxWebsocketFrameInBytes, Logger);
                                                    lock (_activeWebsockets)
                                                    {
                                                        _activeWebsockets.Add(webSocket);
                                                    }
                                                    handler(webSocket);
                                                    lock (_activeWebsockets)
                                                    {
                                                        _activeWebsockets.Remove(webSocket);
                                                    }
                                                }
                                                else
                                                {

                                                }

                                                return;
                                            }

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

                                            if (request.method == HTTPRequest.Method.Post)
                                            {
                                                if (!ParsePost(request, client))
                                                {
                                                    Logger(LogLevel.Error, "Failed parsing post data");
                                                    return;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Logger(LogLevel.Error, "Failed parsing request method");
                                            return;
                                        }

                                        string setCookie;
                                        request.session = GetSession(request, out setCookie);

                                        if (requestCount == 0)
                                        {
                                            keepAlive = request.headers.ContainsKey("connection") && request.headers["connection"].Equals("keep-alive", StringComparison.OrdinalIgnoreCase);

                                            if (setCookie != null && OnNewVisitor != null)
                                            {
                                                Logger(LogLevel.Debug, "Handling visitors...");
                                                OnNewVisitor(request);
                                            }
                                        }

                                        Logger(LogLevel.Debug, "Handling request...");

                                        HTTPResponse response;
                                        try
                                        {

                                            response = HandleRequest(request);

                                            if (response == null || response.bytes == null)
                                            {
                                                Logger(LogLevel.Debug, $"Got no response...");
                                                var errorObj = OnNotFound(request);
                                                response = ResponseFromObject(errorObj, HTTPCode.NotFound);
                                            }
                                            else
                                            {
                                                Logger(LogLevel.Debug, $"Got response with {response.bytes.Length} bytes...");
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            response = OnException(e);
                                        }

                                        response.headers["Content-Length"] = response.bytes != null ? response.bytes.Length.ToString() : "0";

                                        if (response.code == HTTPCode.OK && keepAlive)
                                        {
                                            response.headers["Connection"] = "keep-alive";
                                        }

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
                                            //Logger(LogLevel.Debug, "Cookie inspection**************");
                                            var emptyCookie = request.session.IsEmpty;
                                            if (emptyCookie)
                                            {
                                                Logger(LogLevel.Debug, "Cookie is empty");
                                            }
                                            else
                                            {
                                                Logger(LogLevel.Debug, $"Cookie has {request.session.Size} items");
                                                /*foreach (var entry in request.session.Data)
                                                {
                                                    Logger(LogLevel.Debug, entry.Key + " => " + entry.Value);
                                                }*/
                                            }

                                            if (!emptyCookie)
                                            {
                                                if (Settings.Environment == ServerEnvironment.Prod && !setCookie.Contains("SameSite"))
                                                {
                                                    setCookie = $"{setCookie}; SameSite=None; Secure";
                                                }

                                                response.headers["Set-Cookie"] = setCookie;
                                            }
                                        }
                                        /*else
                                        {
                                            Logger(LogLevel.Debug, "No cookie available :((((((((");
                                        }*/

                                        var answerString = (response.code == HTTPCode.Redirect || response.code == HTTPCode.OK) ? "Found" : "Not Found";
                                        var head = "HTTP/1.1 " + (int)response.code + " " + answerString;
                                        WriteString(writer, head);
                                        foreach (var header in response.headers)
                                        {
                                            WriteString(writer, header.Key + ": " + header.Value);
                                            //Logger(LogLevel.Debug, "Sending header: " + header.Key + " => " + header.Value);
                                        }

                                        WriteNewLine(writer);

                                        writer.Write(response.bytes);
                                    }
                                    else
                                    {
                                        Logger(LogLevel.Error, "Failed parsing request data");
                                    }

                                    requestCount++;
                                } while (keepAlive);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger(LogLevel.Error, e.ToString());
            }
            finally
            {
                client.Close();
            }
        }

        private bool ParsePost(HTTPRequest request, Socket client)
        {
            if (!request.headers.ContainsKey("Content-Length"))
            {
                return true;
            }

            int bodySize;

            var lenStr = request.headers["Content-Length"];
            if (lenStr.Contains("\0"))
            {
                throw new NullByteInjectionException();
            }

            int.TryParse(lenStr, out bodySize);

            if (bodySize < 0 || bodySize > Settings.MaxPostSizeInBytes)
            {
                return false;
            }

            if (bodySize > 0 && request.bytes == null)
            {
                return false;
            }

            if (bodySize > request.bytes.Length)
            {
                return false;
            }

            var contentTypeHeader = request.headers.ContainsKey("Content-Type") ? request.headers["Content-Type"] : "application/x-www-form-urlencoded; charset=UTF-8";

            if (contentTypeHeader.Contains("\0"))
            {
                throw new NullByteInjectionException();
            }

            contentTypeHeader = contentTypeHeader.ToLowerInvariant();

            if (contentTypeHeader.StartsWith("multipart/form-data"))
            {
                var parser = new MultipartParser(new MemoryStream(request.bytes), (key, val) =>
                {
                    var value = val.UrlDecode();
                    if (value.Contains("\0"))
                    {
                        throw new NullByteInjectionException();
                    }
                    request.args[key] = value;
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
                    if (key.Contains("\0"))
                    {
                        throw new NullByteInjectionException();
                    }

                    if (kvpsParts.Length == 1 && keyValuePairStrings.Length == 1)
                    {
                        request.postBody = key;
                        break;
                    }

                    var value = kvpsParts.Length >= 2 ? System.Net.WebUtility.UrlDecode(kvpsParts[1]) : "";

                    // Simply set the key to the parsed value
                    value = value.UrlDecode();
                    if (value.Contains("\0"))
                    {
                        throw new NullByteInjectionException();
                    }

                    request.args[key] = value;
                }
            }
            else
            {
                var encoding = System.Text.Encoding.UTF8; //request.headers["Content-Encoding"]

                request.postBody = encoding.GetString(request.bytes);

                if (request.postBody.Contains("\0"))
                {
                    throw new NullByteInjectionException();
                }
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

        public HTTPResponse HandleRequest(HTTPRequest request, int index = 0)
        {
            Logger(LogLevel.Debug, $"Router find {request.method}=>{request.url}");
            var route = _router.Find(request.method, request.path, request.args);

            if (route != null)
            {
                Logger(LogLevel.Debug, "Calling route handler...");

                object obj = null;

                if (request.method == HTTPRequest.Method.Get && Settings.CacheResponseTime > 0)
                {
                    obj = _requestCache.GetCachedResponse(request.path, Settings.CacheResponseTime);
                }

                if (obj == null)
                {
                    foreach (var entry in route.Handlers)
                    {
                        obj = entry.Handler(request);
                        if (obj != null)
                        {
                            break;
                        }
                    }
                }

                if (obj == null)
                {
                    return null;
                }

                HTTPResponse result = ResponseFromObject(obj, HTTPCode.OK);

                if (result != null && request.method == HTTPRequest.Method.Get && Settings.CacheResponseTime > 0)
                {
                    _requestCache.PutCachedResponse(request.path, result);
                }

                return result;
            }
            else
            if (request.method == HTTPRequest.Method.Options)
            {
                return HTTPResponse.Options();
            }
            else
            {
                Logger(LogLevel.Debug, "Route handler not found...");
            }

            if (_assetCache != null)
            {
                _assetCache.Update();

                return _assetCache.GetFile(request);
            }

            return null;
        }

        public HTTPResponse ResponseFromObject(object obj, HTTPCode code)
        {            
            if (obj is HTTPResponse)
            {
                return (HTTPResponse)obj;
            }
            
            if (obj is string)
            {
                return HTTPResponse.FromString((string)obj, code, Settings.Compression);
            }
            
            if (obj is byte[])
            {
                return HTTPResponse.FromBytes((byte[])obj);
            }
            
            if (obj is DataNode)
            {
                var root = (DataNode)obj;
                var json = JSONWriter.WriteToString(root);
                return HTTPResponse.FromString(json, code, Settings.Compression, "application/json");
            }

            var type = obj.GetType();
            Logger(LogLevel.Error, $"Can't serialize object of type '{type.Name}' into HTTP response");
            return null;
        }

        public static string GenerateWebSocketKey(string input)
        {
            var output = input + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

            using (var sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(output));
                return System.Convert.ToBase64String(hash);
            }
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
                string[] cookies = requestCookies.Split(';' /*, ',' }, StringSplitOptions.RemoveEmptyEntries*/);

                for (int i = cookies.Length - 1; i >= 0; i--)
                {
                    var cookie = cookies[i];

                    string[] s = cookie.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    string name = s[0].Trim();
                    string val = s.Length > 1 ? s[1] : "";

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
                Logger(LogLevel.Debug, $"Created cookie: {cookieValue}");
            }
            else
            if (SessionStorage.HasSession(cookieValue))
            {
                Logger(LogLevel.Debug, $"Loaded session: {cookieValue}");
                session = SessionStorage.GetSession(cookieValue);
                session.lastActivity = DateTime.Now;
                return session;
            }
            else
            {
                session = SessionStorage.CreateSession(cookieValue);
                setCookie = SessionCookieName + "=" + cookieValue;
                Logger(LogLevel.Debug, $"Created session: {cookieValue}");
                return session;
            }

            return session;
        }

        #endregion

        #region HANDLERS
        public void RegisterHandler(HTTPRequest.Method method, string path, int priority, Func<HTTPRequest, object> handler)
        {
            if (Running)
            {
                throw new Exception("Can't register new handlers when running");
            }

            _router.Register(method, path, priority, handler);
        }

        public void Get(string path, Func<HTTPRequest, object> handler)
        {
            RegisterHandler(HTTPRequest.Method.Get, path, 0, handler);
        }

        public void Post(string path, Func<HTTPRequest, object> handler)
        {
            RegisterHandler(HTTPRequest.Method.Post, path, 0, handler);
        }

        public void Put(string path, Func<HTTPRequest, object> handler)
        {
            RegisterHandler(HTTPRequest.Method.Put, path, 0, handler);
        }

        public void Delete(string path, Func<HTTPRequest, object> handler)
        {
            RegisterHandler(HTTPRequest.Method.Delete, path, 0, handler);
        }

        private string MakeWebSocketKeyPair(string protocol, string path)
        {
            if (!path.StartsWith("/"))
            {
                path = "/" + path;
            }

            if (protocol == null)
            {
                return path;
            }

            return $"{protocol}:{path}";
        }

        public void WebSocket(string path, Action<WebSocket> handler, params string[] protocols)
        {
            if (protocols == null || protocols.Length == 0)
            {
                var key = MakeWebSocketKeyPair(null, path);
                _websocketsHandlers[key] = handler;
            }
            else
            {
                foreach (var protocol in protocols)
                {
                    var key = MakeWebSocketKeyPair(protocol, path);
                    _websocketsHandlers[key] = handler;
                }
            }
        }

        internal void AddPlugin(ServerPlugin plugin)
        {
            this._plugins.Add(plugin);
        }
        #endregion
    }
}
