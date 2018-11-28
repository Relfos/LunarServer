namespace LunarLabs.WebServer.Templates
{
    public class LiteralKey : RenderingKey
    {
        private object _value;

        private RenderingType _type;
        public override RenderingType RenderingType => _type;

        public LiteralKey(object value, RenderingType type)
        {
            this._value = value;
            this._type = type;
        }

        public override object Evaluate(RenderingContext context)
        {
            return _value;
        }

        public override string ToString()
        {
            return _value.ToString();
        }
    }
}
