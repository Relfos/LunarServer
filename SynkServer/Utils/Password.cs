using System;
using System.Collections.Generic;
using System.Text;

namespace SynkServer.Utils
{
    public static class PasswordUtils
    {
        public static string GetPasswordHash(this string password)
        {
            var key = password.MD5().ToLower();
            var s = "./ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789'";
            s = s.Shuffle();
            s = s.Substring(s.Length - 4);
            var salt = "$1$" + s;
            return Crypt.crypt(key, salt);
        }

        public static bool CheckPassword(this string password, string user_hash)
        {
            var password_md5 = password.MD5();

            if (string.IsNullOrEmpty(user_hash))
            {
                return false;
            }

            var temp_hash = Crypt.crypt(password_md5.ToLower(), user_hash);
            return temp_hash.Equals(user_hash);
        }

        public static string Shuffle(this string str)
        {
            char[] array = str.ToCharArray();
            Random rng = new Random();
            int n = array.Length;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                var value = array[k];
                array[k] = array[n];
                array[n] = value;
            }
            return new string(array);
        }

    }
}
