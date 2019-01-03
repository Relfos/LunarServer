namespace LunarLabs.Templates
{
    public class SelfKey : RenderingKey
    {
        public override RenderingType RenderingType => RenderingType.Any;

        public override object Evaluate(RenderingContext context)
        {
            return context.DataStack[context.DataStack.Count - 1];
        }

        public override string ToString()
        {
            return "this";
        }
    }
}
