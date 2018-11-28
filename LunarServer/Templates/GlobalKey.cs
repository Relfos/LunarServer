using System;
using System.Collections.Generic;
using System.Text;

namespace LunarLabs.WebServer.Templates
{
    public class GlobalKey : RenderingKey
    {
        private string _global;

        public override RenderingType RenderingType => RenderingType.Any;

        public GlobalKey(string key)
        {
            this._global = key;
        }

        public override object Evaluate(RenderingContext context)
        {
            return context.Get(_global);
        }
    }
}
