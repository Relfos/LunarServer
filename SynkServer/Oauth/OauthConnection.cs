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

    public abstract class OauthConnection
    {
        public static string OAUTH_ID = "KEeF3KEYWfZI53sSTAf22";

        public string token { get; protected set; }

        public Logger log { get; private set; }

        public OauthProfile profile { get; protected set; }

        public bool IsAuthenticated { get { return token != null; } }

        protected string client_id;
        protected string client_secret;
        protected string redirect_uri; //Should match Site URL        

//        protected string app_url = "http://localhost";

        public OauthConnection(Logger log, string client_id, string client_secret, string redirect_uri, string token = null)
        {
            this.log = log;
            this.client_id = client_id;
            this.client_secret = client_secret;
            this.redirect_uri = redirect_uri;
            this.token = token;
        }

        protected string GetRedirectURL()
        {
            return redirect_uri.UrlEncode();
        }

        public abstract string GetLoginURL();

        public abstract bool Login(string code);

        public void Logout()
        {
            this.token = null;
        }


    }

}
