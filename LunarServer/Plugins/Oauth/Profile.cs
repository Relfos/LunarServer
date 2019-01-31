using LunarLabs.Parser;
using LunarLabs.WebServer.Core;
using LunarLabs.WebServer.HTTP;

namespace LunarLabs.WebServer.Plugins.Oauth
{
    public class Profile
    {
        public const string SessionPrefix = "profile_";

        public string id;
        public string name;
        public string email;
        public string photo;
        public string birthday;

        public string token;
        public DataNode data; // TODO not serialized

        public void Save(Session session)
        {
            session.SetString(SessionPrefix + "id", id);
            session.SetString(SessionPrefix + "token", token);
            session.SetString(SessionPrefix + "name", name);
            session.SetString(SessionPrefix + "email", email);
            session.SetString(SessionPrefix + "photo", photo);
            session.SetString(SessionPrefix + "birthday", birthday);
        }

        public static void Remove(Session session)
        {
            session.Remove(SessionPrefix + "id");
            session.Remove(SessionPrefix + "token");
            session.Remove(SessionPrefix + "name");
            session.Remove(SessionPrefix + "email");
            session.Remove(SessionPrefix + "photo");
            session.Remove(SessionPrefix + "birthday");
        }

        public static Profile Load(Session session)
        {
            var id = session.GetString(SessionPrefix + "id", null);

            if (id == null)
            {
                return null;
            }

            var profile = new Profile();
            profile.id = id;
            profile.token = session.GetString(SessionPrefix + "token");
            profile.name = session.GetString(SessionPrefix + "name");
            profile.email = session.GetString(SessionPrefix + "email");
            profile.photo = session.GetString(SessionPrefix + "photo");
            profile.birthday = session.GetString(SessionPrefix + "birthday");
            return profile;
        }
    }

    public static class ProfileUtils
    {
        public static Profile GetProfile(this HTTPRequest request)
        {
            return Profile.Load(request.session);
        }

        public static bool IsAuthenticated(this HTTPRequest request)
        {
            return request.GetProfile() != null;
        }

        public static bool Logout(this HTTPRequest request)
        {
            if (request.IsAuthenticated())
            {
                Profile.Remove(request.session);
                return true;
            }

            return false;
        }
    }
}
