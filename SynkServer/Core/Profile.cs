using LunarParser;
using SynkServer.HTTP;
using System;
using System.Collections.Generic;
using System.Text;

namespace SynkServer.Oauth
{
    public class Profile
    {
        public const string sessionKey = "_profile";

        public string id;
        public string name;
        public string email;
        public string pictureURL;

        public string token;
        public DataNode data;
    }

    public static class ProfileUtils
    {
        public static Profile GetProfile(this HTTPRequest request)
        {
            if (request.session.Contains(Profile.sessionKey))
            {
                return (Profile)request.session.Get(Profile.sessionKey);
            }

            return null;
        }

        public static bool IsAuthenticated(this HTTPRequest request)
        {
            return request.GetProfile() != null;
        }

        public static bool Logout(this HTTPRequest request)
        {
            if (request.IsAuthenticated())
            {
                request.session.Remove(Profile.sessionKey);
                return true;
            }

            return false;
        }
    }
}
