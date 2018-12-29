using LunarLabs.Parser;
using LunarLabs.WebServer.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace LunarLabs.WebServer.Oauth
{
    public enum OauthKind
    {
        Facebook,
        LinkedIn
    }

    public abstract class OauthConnection
    {
        public static string OAUTH_ID = "KEeF3KEYWfZI53sSTAf22";

        public LoggerCallback logger { get; private set; }

        public string localPath { get; private set; }//Should match Site URL        

        protected string client_id;
        protected string client_secret;
        protected string app_url;

        public OauthConnection(LoggerCallback log, string app_url, string client_id, string client_secret, string localPath)
        {
            if (!app_url.StartsWith("http://"))
            {
                app_url = "http://" + app_url;
            }

            this.logger = log;
            this.app_url = app_url;
            this.client_id = client_id;
            this.client_secret = client_secret;
            this.localPath = localPath;
        }

        public abstract OauthKind GetKind();

        public abstract string GetLoginURL();

        public abstract Profile Login(string code);

        public string GetRedirectURL()
        {
            var redirect_url = app_url + "/" + localPath;
            return redirect_url.UrlEncode();
        }

    }

}
