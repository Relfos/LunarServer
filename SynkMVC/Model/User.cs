using System;
using System.Collections.Generic;

namespace SynkMVC.Model
{
    public class User : Entity
    {
        public override void InitFields()
        {		
		    this.RegisterField("username").asName(30).showInGrid();
		    this.RegisterField("password").asPassword(40).makeOptional();
		    this.RegisterField("hash").asHash("password");
            this.RegisterField("permissions").asBitfield();
            this.RegisterField("database").asString(64);
            this.RegisterField("payload").asText();            
        }

        public override string ToString()
        {
            return GetFieldValue("username");
        }

        public override Condition GetSearch(string term)
        {
            return Condition.Contains("username", term);
        }

        public long GetPermissions()
        {
            long result;
            long.TryParse(GetFieldValue("permissions"), out result);
            return result;
        }

        public bool HasPermission(int permissionIndex)
        {
            var perms = GetPermissions();
            int flag = 1 << permissionIndex;
            return (perms & flag) != 0;
        }

    }
}
