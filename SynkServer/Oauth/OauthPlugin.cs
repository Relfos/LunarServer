using SynkServer.Core;
using SynkServer.HTTP;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SynkServer.Oauth
{

    public class OauthPlugin: SitePlugin
    {
        private Dictionary<OauthKind, OauthConnection> _auths = new Dictionary<OauthKind, OauthConnection>();
        public IEnumerable<OauthConnection> auths { get { return _auths.Values; } }

        private Logger log;

        public Func<OauthKind, Profile, object> OnLogin;
        public Func<OauthKind, object> OnError;

        private string app_url;

        public OauthPlugin(Logger log, string app_url)
        {
            this.log = log;
            this.app_url = app_url;
            this.OnLogin = OnLoginException;
            this.OnError = OnErrorLog;
        }

        private object OnErrorLog(OauthKind kind)
        {
            log.Error("Auth failed for " + kind);
            return null;
        }

        private object OnLoginException(OauthKind kind, Profile profile)
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
            switch (kind)
            {
                case OauthKind.Facebook: return new FacebookAuth(log, app_url, client_id, client_secret, redirect_uri);
                case OauthKind.LinkedIn: return new LinkedInAuth(log, app_url, client_id, client_secret, redirect_uri);
                default: return null;
            }
        }

        public override bool Install(Site site, string path)
        {
            this.site = site;

            foreach (var auth in _auths.Values)
            {
                site.Get(Combine(path, auth.localPath), request =>
                {
                    var kind = auth.GetKind();

                    if (request.HasVariable("code"))
                    {
                        var profile = auth.Login(request.args["code"]);
                        if (profile != null)
                        {
                            request.session.Set(Profile.sessionKey, profile);
                            return OnLogin(kind, profile);                            
                        }
                    }

                    return OnError(kind);
                });
            }

            return true;
        }
    }
}
