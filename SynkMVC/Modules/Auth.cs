using SynkMVC;
using SynkMVC.Model;
using System;
using System.Collections.Generic;

namespace SynkMVC.Modules
{
    public class Auth: Module
    {
        public bool checkPassword(string password, string user_hash)
        {		
		    var password_md5 = password.MD5();

            if (string.IsNullOrEmpty(user_hash))
            {
                return false;
            }

            var temp_hash = Crypt.crypt(password_md5.ToLower(), user_hash);
            return temp_hash.Equals(user_hash);
        }

        public void OnDefault(SynkContext context)
        {
            context.PushTemplate("auth/default");
            context.Render();
        }

        private void ShowDefaultPage(SynkContext context)
        {
            context.ChangeModule(context.config.GetFieldValue("defaultModule"));
            context.ChangeAction("default");
            context.Reload();
            context.RunController();
        }

        public void OnLogin(SynkContext context)
        {
            var username = context.loadVarFromRequest("username");
	        var password = context.loadVarFromRequest("password");
	   
	        var dbName = context.config.GetFieldValue("database");
	        var cond = Condition.Equal("username",  username);
            var user = context.database.FetchEntity<User>(cond);

            string hash = null;

            if (user != null && user.exists)
            {
                hash = user.GetFieldValue("hash");
            }

            if (context.database.failed)
            {
                context.warning = "Database error!";
                context.PushTemplate("auth/default");
            }
            else
            if (user != null && user.exists && (string.IsNullOrEmpty(hash) || this.checkPassword(password, hash)))
	        {
                if (context.config.GetFieldBool("instanced")) {
				    dbName = user.GetFieldValue("database");
                }

                context.LogIn(user.id, dbName);

                ShowDefaultPage(context);
                return;
            }
	        else
	        {
                if (user.exists)
                {
                    context.warning = "Dados de login invalidos!";
                }
                else
                {
                    context.warning = "Utilizador não existente!";
                }

                context.PushTemplate("auth/default");
            }

            context.Render();
        }

        public void OnLogout(SynkContext context)
        {
		    context.LogOut();
            ShowDefaultPage(context);
            return;
        }

    }
}
