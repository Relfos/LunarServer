using System;

namespace LunarLabs.WebServer.Templates
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

            throw new Exception("Expected bool key");
        }

        public override string ToString()
        {
            return "!"+body.ToString();
        }
    }
}
