namespace LunarLabs.Templates
{
    public class GlobalKey : PathRenderingKey
    {
        private string _global;

        public override RenderingType RenderingType => RenderingType.Any;

        public GlobalKey(string key) : base(key)
        {
            this._global = steps[0];
            this.startingStep = 1;
        }

        public override object Evaluate(RenderingContext context)
        {
            var obj = context.Get(_global);

            if (this.steps.Length > 1)
            {
                int stackPointer = context.DataStack.Count - 1;
                var temp = context.DataStack[stackPointer];

                context.DataStack[stackPointer] = obj;

                var result = base.Evaluate(context);

                context.DataStack[stackPointer] = temp;

                return result;
            }

            return obj;
        }
    }
}
