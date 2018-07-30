using LunarLabs.WebServer.Core;
using LunarLabs.WebServer.HTTP;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LunarLabs.WebServer.Oauth
{

    public class OauthPlugin: SitePlugin
    {
        private Dictionary<OauthKind, OauthConnection> _auths = new Dictionary<OauthKind, OauthConnection>();
        public IEnumerable<OauthConnection> auths { get { return _auths.Values; } }

        private Logger log { get { return this.site.log; } }

        public Func<OauthKind, HTTPRequest, object> OnLogin;
        public Func<OauthKind, HTTPRequest, object> OnError;

        public OauthPlugin(Site site, string rootPath = null) : base(site, rootPath)
        { 
            this.OnLogin = OnLoginException;
            this.OnError = OnErrorLog;
        }

        private object OnErrorLog(OauthKind kind, HTTPRequest request)
        {
            log.Error("Auth failed for " + kind);
            return null;
        }

        private object OnLoginException(OauthKind kind, HTTPRequest request)
        {
            throw new NotImplementedException();
        }

        public void AddAuth(OauthKind kind, string client_id, string client_secret)
        {
            var redirect_uri = $"{kind.ToString().ToLowerInvariant()}_auth";
            _auths[kind] = Create(kind, log, client_id, client_secret, redirect_uri);
        }

        public OauthConnection Create(OauthKind kind, Logger log, string client_id, string client_secret, string redirect_uri, string token = null)
        {
            var app_url = this.site.server.settings.environment == ServerEnvironment.Prod ? this.site.host : "http://localhost";

            switch (kind)
            {
                case OauthKind.Facebook: return new FacebookAuth(log, app_url, client_id, client_secret, redirect_uri);
                case OauthKind.LinkedIn: return new LinkedInAuth(log, app_url, client_id, client_secret, redirect_uri);
                default: return null;
            }
        }

        public override bool Install()
        {
            foreach (var auth in _auths.Values)
            {
                var path = Combine(auth.localPath);
                site.Get(path, request =>
                {
                    var kind = auth.GetKind();

                    if (request.HasVariable("code"))
                    {
                        var profile = auth.Login(request.args["code"]);
                        if (profile != null)
                        {
                            request.session.Set(Profile.sessionKey, profile);
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
