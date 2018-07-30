using System;
using System.IO;

namespace LunarLabs.WebMVC.Model
{
    public class Config: Entity
    {
        public override void InitFields()
        {
            this.RegisterField("logFile").asString(200).makeOptional();

            this.RegisterField("database").asString(200);
            this.RegisterField("instanced").asBoolean();

            this.RegisterField("sqlPlugin").asString(200).setDefaultValue("mysql");
            this.RegisterField("sqlHost").asString(200).setDefaultValue("localhost");
            this.RegisterField("sqlUser").asString(200).setDefaultValue("root");
            this.RegisterField("sqlPass").asString(200).makeOptional();

            this.RegisterField("defaultUser").asString(200).setDefaultValue("admin");
            this.RegisterField("defaultPass").asString(200).setDefaultValue("test");

            this.RegisterField("defaultLanguage").asString(200).setDefaultValue("en");
            this.RegisterField("defaultModule").asString(200).setDefaultValue("home");
        }

        public virtual void Load(MVC hostData)
        {
            var fileName = hostData.GetFullPath("config.txt");
            var lines = System.IO.File.ReadAllLines(fileName);
            foreach (var temp in lines)
            {
                var line = temp;

                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                if (line.Contains("#"))
                {
                    line = line.Split('#')[0];

                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }
                }

                var s = line.Split('=');
                if (s.Length >= 2)
                {
                    var fieldName = s[0];
                    var fieldValue = s[1];
                    this.SetFieldValue(fieldName, fieldValue);
                }
            }
        }

        public virtual void InitFirstUser(SynkContext context, User user)
        {
            user.SetFieldValue("username", this.GetFieldValue("defaultUser"));
            user.SetFieldValue("password", this.GetFieldValue("defaultPass"));
            user.SetFieldValue("permissions", 0xFFFFFFFF.ToString());            

            var dbName = this.GetFieldValue("database");

            bool instanced = this.GetFieldBool("instanced");
            if (instanced)
            {
                user.SetFieldValue("database", dbName + '_' + Utility.GetUniqID());
            }
            else
            {
                user.SetFieldValue("database", dbName);
            }
        }

    }
}
