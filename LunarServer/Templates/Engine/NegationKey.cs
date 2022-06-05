using System;
using System.Collections;
using System.Linq;

namespace LunarLabs.Templates
{
    public class NegationKey : RenderingKey
    {
        private RenderingKey body;

        public override RenderingType RenderingType => RenderingType.Bool;

        public NegationKey(RenderingKey body) : base()
        {
            this.body = body;
        }

        public override object Evaluate(RenderingContext context)
        {
            var obj = body.Evaluate(context);

            if (obj is bool)
            {
                return !((bool)obj);
            }

            if (obj is IEnumerable)
            {
                var collection = (IEnumerable)obj;
                return !collection.Any();
            }

            throw new Exception("Expected bool key");
        }

        public override string ToString()
        {
            return "!"+body.ToString();
        }
    }
}
