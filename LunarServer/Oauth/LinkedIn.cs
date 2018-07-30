using LunarLabs.Parser;
using LunarLabs.Parser.JSON;
using LunarLabs.WebServer.Core;
using LunarLabs.WebServer.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace LunarLabs.WebServer.Oauth
{
    public class LinkedInAuth : OauthConnection
    {
        public LinkedInAuth(Logger log, string app_url, string client_id, string client_secret, string redirect_uri)  : base(log, app_url, client_id, client_secret, redirect_uri)
        {
        }

        public override OauthKind GetKind()
        {
            return OauthKind.LinkedIn;
        }

        public override string GetLoginURL()
        {
            return $"https://www.linkedin.com/oauth/v2/authorization?response_type=code&client_id={client_id}&redirect_uri={GetRedirectURL()}&state={OAUTH_ID}&scope=r_basicprofile%20r_emailaddress";
            //r_fullprofile
        }

        private string Authorize(string code)
        {
            var url = $"https://www.linkedin.com/oauth/v2/accessToken";

            var args = new Dictionary<string, string>();
            args["grant_type"] = "authorization_code";
            args["code"] = code;
            args["redirect_uri"] = GetRedirectURL();
            args["client_id"] = client_id;
            args["client_secret"] = client_secret;

            try
            {
                var json = HTTPUtils.Post(url, args);
                var root = JSONReader.ReadFromString(json);

                return root.GetString("access_token");
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
                return null;
            }
        }

        public override Profile Login(string code)
        {
            var token = Authorize(code);
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            var user = GetUser(null, token);
            if (user != null)
            {
                var profile = new Profile()
                {
                    token = token,
                    id = user.GetString("id"),
                    name = user.GetString("formattedName"),
                    email = user.GetString("emailAddress"),
                    photo = user.GetString("pictureUrl"),
                    birthday = user.GetString("date-of-birth"),
                    data = user
                };
                return profile;
            }

            return null;
        }

        public DataNode GetUser(string userid, string token)
        {
            try
            {
                if (userid == null) { userid = "~"; }
                var url = $"https://api.linkedin.com/v1/people/{userid}:(id,formatted-name,picture-url,email-address)?format=json";
                var headers = new Dictionary<string, string>();
                headers["Authorization"] = "Bearer " + token;

                var json = HTTPUtils.Get(url, headers);
                var root = JSONReader.ReadFromString(json);

                return root;
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
                return null;
            }
        }


    }
}
