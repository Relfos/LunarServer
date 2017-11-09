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

    public class FacebookAuth
    {
        private string client_id;
        private string client_secret;
        public string app_url = "http://localhost";

        public bool IsAuthenticated { get { return token != null; } }


        private const string authorization_base_url = "https://www.facebook.com/dialog/oauth";
        private const string token_url = "https://graph.facebook.com/oauth/access_token";
        private string redirect_uri; //Should match Site URL        

        private string token;

        private Logger log;

        public FacebookAuth(Logger log, string client_id, string client_secret, string redirect_uri)
        {
            this.log = log;
            this.client_id = client_id;
            this.client_secret = client_secret;
            this.redirect_uri = redirect_uri;
        }

        public void Logout()
        {
            this.token = null;
        }

        public string GetLoginURL()
        {
            var url = authorization_base_url + "?response_type=code&redirect_uri="+redirect_uri.UrlEncode()+"&client_id=" + client_id;
            //url += "&auth_type=rerequest";
            url += "&scope=email,public_profile"; //user_birthday,
            return url;
        }

        private string Authorize(string code)
        {
            var url = $"{token_url}?client_id={client_id}&redirect_uri={redirect_uri.UrlEncode()}&client_secret={client_secret}&code={code}";

            try
            {
                var json = Utils.DownloadString(url);
                var root = JSONReader.ReadFromString(json);

                return root.GetString("access_token");
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
                return null;
            }
        }

        public DataNode profile { get; private set; }

        public bool Login(string code)
        {
            this.token = Authorize(code);
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            profile = GetUser(null, new FacebookField[] { FacebookField.Id, FacebookField.Name, FacebookField.Gender, FacebookField.Picture, FacebookField.Email } );
            return profile != null;
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
                var json = Utils.DownloadString(url);
                System.IO.File.WriteAllText("output.json", json);
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