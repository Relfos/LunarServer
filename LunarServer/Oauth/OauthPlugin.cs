using LunarLabs.WebServer.Core;
using LunarLabs.WebServer.HTTP;
using System;
using System.Collections.Generic;

namespace LunarLabs.WebServer.Oauth
{

    public class OauthPlugin: ServerPlugin
    {
        private Dictionary<OauthKind, OauthConnection> _auths = new Dictionary<OauthKind, OauthConnection>();
        public IEnumerable<OauthConnection> auths { get { return _auths.Values; } }

        private LoggerCallback logger { get { return this.Server.Logger; } }

        public Func<OauthKind, HTTPRequest, object> OnLogin;
        public Func<OauthKind, HTTPRequest, object> OnError;

        public OauthPlugin(HTTPServer server, string rootPath = null) : base(server, rootPath)
        { 
            this.OnLogin = OnLoginException;
            this.OnError = OnErrorLog;
        }

        private object OnErrorLog(OauthKind kind, HTTPRequest request)
        {
            logger(LogLevel.Error, "Auth failed for " + kind);
            return null;
        }

        private object OnLoginException(OauthKind kind, HTTPRequest request)
        {
            throw new NotImplementedException();
        }

        public void AddAuth(OauthKind kind, string client_id, string client_secret)
        {
            var redirect_uri = $"{kind.ToString().ToLowerInvariant()}_auth";
            _auths[kind] = Create(kind, logger, client_id, client_secret, redirect_uri);
        }

        public OauthConnection Create(OauthKind kind, LoggerCallback logger, string client_id, string client_secret, string redirect_uri, string token = null)
        {
            var app_url = this.Server.Settings.Host;

            if (!app_url.StartsWith("http://"))
            {
                app_url = "http://" + app_url;
            }

            switch (kind)
            {
                case OauthKind.Facebook: return new FacebookAuth(logger, app_url, client_id, client_secret, redirect_uri);
                case OauthKind.LinkedIn: return new LinkedInAuth(logger, app_url, client_id, client_secret, redirect_uri);
                default: return null;
            }
        }

        protected override bool OnInstall()
        {
            foreach (var auth in _auths.Values)
            {
                var path = this.Path + auth.localPath;
                Server.Get(path, request =>
                {
                    var kind = auth.GetKind();

                    if (request.HasVariable("code"))
                    {
                        var profile = auth.Login(request.args["code"]);
                        if (profile != null)
                        {
                            profile.Save(request.session);
                            return OnLogin(kind, request);
                        }
                    }

                    return OnError(kind, request);
                });
            }

            return true;
        }
    }
}
