using System;
using System.Collections.Generic;

namespace LunarLabs.WebMVC.Model
{
    public class File : Entity
    {
        public override void InitFields()
        {
            this.RegisterField("real_name").asString(60).showInGrid();
            this.RegisterField("hash").asString(40);
            this.RegisterField("size").asSize().showInGrid();
            this.RegisterField("local_name").asString(80).showInGrid();
            this.RegisterField("thumb").asString(80);
        }

        public override string ToString()
        {
            return GetFieldValue("real_name");
        }    

        public byte[] GetBytes(SynkContext context)
        {
            var content = GetFieldValue("local_name");

            var filePath = System.IO.Path.Combine("public", content.TrimStart('/', '\\'));
            filePath = context.site.GetFullPath(filePath);
            if (System.IO.File.Exists(filePath))
            {
                return System.IO.File.ReadAllBytes(filePath);
            }
            else
            {
                return null;
            }
        }

    }
}
