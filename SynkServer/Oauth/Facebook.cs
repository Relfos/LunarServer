using LunarParser;
using LunarParser.JSON;
using SynkServer.Core;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SynkServer.Oauth
{
    public enum FacebookField
    {
        Id,
        Name,
        Gender,        
        Picture, 
        Email
    }

    public class FacebookAuth:OauthConnection
    {     
        private const string authorization_base_url = "https://www.facebook.com/dialog/oauth";
        private const string token_url = "https://graph.facebook.com/oauth/access_token";
        
        public FacebookAuth(Logger log, string client_id, string client_secret, string redirect_uri)  : base(log, client_id, client_secret, redirect_uri)
        {
        }

        public override string GetLoginURL()
        {
            var url = authorization_base_url + "?response_type=code&redirect_uri="+redirect_uri.UrlEncode()+"&client_id=" + client_id;
            //url += "&auth_type=rerequest";
            url += "&scope=email,public_profile"; //user_birthday,
            return url;
        }

        private string Authorize(string code)
        {
            var url = $"{token_url}?client_id={client_id}&redirect_uri={GetRedirectURL()}&client_secret={client_secret}&code={code}";

            try
            {
                var json = Utils.HTTPGet(url);
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

            var user = GetUser(null, new FacebookField[] { FacebookField.Id, FacebookField.Name, FacebookField.Gender, FacebookField.Picture, FacebookField.Email } );
            if (user != null) {
                this.profile = new OauthProfile()
                {
                    id = user.GetString("id"),
                    name = user.GetString("name"),
                    email = user.GetString("email"),
                    pictureURL = user.GetNode("picture").GetNode("data").GetString("url"),
                    data = user
                };
            }
            return user != null;
        }

        public DataNode GetUser(string userid, IEnumerable<FacebookField> fields)
        {
            try
            {
                string fieldStr = "";
                foreach (var field in fields)
                {
                    if (fieldStr.Length>0) { fieldStr += ","; }

                    var fieldName = field.ToString();
                    fieldName = char.ToLowerInvariant(fieldName[0]) + fieldName.Substring(1);
                    fieldStr += fieldName;
                }

                if (userid == null) { userid = "me"; }
                var url = $"https://graph.facebook.com/{userid}?access_token={token}&fields={fieldStr}";
                var json = Utils.HTTPGet(url);
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