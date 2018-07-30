using System;
using System.Collections.Generic;
using System.IO;

namespace LunarLabs.WebMVC.Model
{
    public class Enum : Entity
    {
        public override void InitFields()
        {
            this.RegisterField("name").asString(60).showInGrid();
            this.RegisterField("values").asText();
        }

        public override string ToString()
        {
            return GetFieldValue("name");
        }

    }
}
