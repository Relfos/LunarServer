using LunarParser;
using SynkServer.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace SynkServer.Oauth
{
    public struct OauthProfile
    {
        public string id;
        public string name;
        public string email;
        public string pictureURL;
        public DataNode data;
    }

    public enum OauthKind
    {
        Facebook,
        LinkedIn
    }

    public abstract class OauthConnection
    {
        public static string OAUTH_ID = "KEeF3KEYWfZI53sSTAf22";

        public string token { get; protected set; }

        public Logger log { get; private set; }

        public OauthProfile profile { get; protected set; }

        public bool IsAuthenticated { get { return token != null; } }

        public string localPath { get; private set; }//Should match Site URL        

        protected string client_id;
        protected string client_secret;
        protected string app_url;

        public OauthConnection(Logger log, string app_url, string client_id, string client_secret, string localPath, string token = null)
        {
            this.log = log;
            this.app_url = app_url;
            this.client_id = client_id;
            this.client_secret = client_secret;
            this.localPath = localPath;
            this.token = token;
        }

        public abstract OauthKind GetKind();

        public abstract string GetLoginURL();

        public abstract bool Login(string code);

        public void Logout()
        {
            this.token = null;
        }

        public string GetRedirectURL()
        {
            var redirect_url = app_url + "/" + localPath;
            return redirect_url.UrlEncode();
        }

    }

}
