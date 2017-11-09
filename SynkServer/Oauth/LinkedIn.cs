using LunarParser;
using LunarParser.JSON;
using SynkServer.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace SynkServer.Oauth
{
    public class LinkedInAuth : OauthConnection
    {
        public LinkedInAuth(Logger log, string client_id, string client_secret, string redirect_uri)  : base(log, client_id, client_secret, redirect_uri)
        {
        }

        public override string GetLoginURL()
        {
            return $"https://www.linkedin.com/oauth/v2/authorization?response_type=code&client_id={client_id}&redirect_uri={GetRedirectURL()}&state={OAUTH_ID}&scope=r_basicprofile%20r_emailaddress";
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
                var json = Utils.HTTPPost(url, args);
                var root = JSONReader.ReadFromString(json);

                return root.GetString("access_token");
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
                return null;
            }
        }

        public override bool Login(string code)
        {
            this.token = Authorize(code);
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            var user = GetUser(null);
            if (user != null)
            {
                this.profile = new OauthProfile()
                {
                    id = user.GetString("id"),
                    name = user.GetString("formattedName"),
                    email = user.GetString("emailAddress"),
                    pictureURL = user.GetString("pictureUrl"),
                    data = user
                };
            }
            return user != null;
        }

        public DataNode GetUser(string userid)
        {
            try
            {
                if (userid == null) { userid = "~"; }
                var url = $"https://api.linkedin.com/v1/people/{userid}:(id,formatted-name,picture-url,email-address)?format=json";
                var headers = new Dictionary<string, string>();
                headers["Authorization"] = "Bearer " + token;

                var json = Utils.HTTPGet(url, headers);
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
