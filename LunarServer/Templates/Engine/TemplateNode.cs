using System;
using System.Collections;
using System.Collections.Generic;
using System.Security;

namespace LunarLabs.Templates
{
    public abstract class TemplateNode
    {
        public readonly Document Document;

        public TemplateNode(Document document)
        {
            this.Document = document;
            document.AddNode(this);
        }

        public abstract void Execute(RenderingContext context);
    }

    public class GroupNode : TemplateNode
    {
        public List<TemplateNode> Nodes = new List<TemplateNode>();

        public GroupNode(Document document) : base(document)
        {

        }

        public override void Execute(RenderingContext context)
        {
            foreach (var node in Nodes)
            {
                node.Execute(context);
            }
        }
    }

    public class TextNode : TemplateNode
    {
        public string content;

        public TextNode(Document document, string content) : base(document)
        {
            this.content = content;
        }

        public override void Execute(RenderingContext context)
        {
            context.output.Append(content);
        }
    }

    public class BodyNode : TemplateNode
    {

        public BodyNode(Document document) : base(document)
        {

        }

        public override void Execute(RenderingContext context)
        {
            var temp = context.DataStack;
            context.DataStack = new List<object>();
            context.DataStack.Add(context.DataRoot);
            var next = context.queue.Dequeue();
            next.Execute(context);
            context.DataStack = temp;
        }
    }

    public class EvalNode : TemplateNode
    {
        public RenderingKey key;
        public bool escape;

        public EvalNode(Document document, string key, bool escape) : base(document)
        {
            this.key = RenderingKey.Parse(key, RenderingType.Any);
            this.escape = escape;
        }

        public override void Execute(RenderingContext context)
        {
            var obj = context.EvaluateObject(key);

            var pointer = context.DataStack[context.DataStack.Count - 1];
            if (obj == null && context != pointer)
            {
                obj = context.EvaluateObject(key);
            }

            if (obj != null)
            {
                string temp;

                if (obj is decimal)
                {
                    temp = ((decimal)obj).ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                if (obj is float)
                {
                    temp = ((float)obj).ToString(".################", System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                if (obj is double)
                {
                    temp = ((double)obj).ToString(".################", System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                {
                    temp = obj.ToString();
                }

                if (escape)
                {
                    temp = SecurityElement.Escape(temp);
                }

                context.output.Append(temp);
            }
        }
    }

    public class SetNode : TemplateNode
    {
        public RenderingKey key;

        public SetNode(Document document, string key) : base(document)
        {
            this.key = RenderingKey.Parse(key, RenderingType.Any);
        }

        public override void Execute(RenderingContext context)
        {
            if (this.key is CompositeRenderingKey)
            {
                var composite = (CompositeRenderingKey)this.key;
                if (composite.Operator == KeyOperator.Assignment)
                {
                    var varName = composite.leftSide.ToString();
                    var val = composite.rightSide.Evaluate(context);
                    context.Set(varName, val);
                }
                else
                {
                    throw new Exception("Expected assignment operator for set node");
                }
            }
            else
            if (this.key is CompositeRenderingKey)
            {
                var varName = key.ToString();
                context.Set(varName, true);
            }
            else
            {

            }
        }
    }

    public class IfNode : TemplateNode
    {
        public RenderingKey condition;
        public TemplateNode trueNode;
        public TemplateNode falseNode;

        public IfNode(Document document, string condition) : base(document)
        {
            this.condition = RenderingKey.Parse(condition, RenderingType.Any);
        }

        public override void Execute(RenderingContext context)
        {
            if (this.trueNode == null)
            {
                throw new Exception("Missing true branch in If node");
            }

            var result = context.EvaluateObject(condition);

            var isFalse = result == null || result.Equals(false) || ((result is string) && ((string)result).Length == 0);

            if (!isFalse && result is IEnumerable)
            {
                isFalse = !((IEnumerable)result).Any();
            }

            if (isFalse)
            {
                if (falseNode != null)
                {
                    falseNode.Execute(context);
                }

                return;
            }

            trueNode.Execute(context);
        }
    }

    public class EachNode : TemplateNode
    {
        public RenderingKey key;

        public TemplateNode inner;

        public EachNode(Document document, string collection) : base(document)
        {
            this.key = RenderingKey.Parse(collection, RenderingType.Collection);
        }

        public override void Execute(RenderingContext context)
        {
            if (inner == null)
            {
                throw new Exception("Missing inner branch in Each node");
            }

            var obj = context.EvaluateObject(key);

            if (obj == null)
            {
                return;
            }

            var list = obj as IEnumerable;

            if (list != null)
            {
                context.operation = RenderingOperation.None;

                int index = 0;
                int last = list.Count() - 1;
                foreach (var item in list)
                {
                    context.Set("index", index);
                    context.Set("first", index == 0);
                    context.Set("last", index == last);
                    context.DataStack.Add(item);
                    inner.Execute(context);
                    context.DataStack.RemoveAt(context.DataStack.Count - 1);

                    if (context.operation == RenderingOperation.Break)
                    {
                        context.operation = RenderingOperation.None;
                        break;
                    }

                    index++;
                }
            }
            else
            {
                context.DataStack.Add(obj);
                context.Set("index", 0);
                context.Set("first", true);
                context.Set("last", true);
                inner.Execute(context);
                context.DataStack.RemoveAt(context.DataStack.Count - 1);
            }
        }
    }

    public class BreakNode : TemplateNode
    {
        public BreakNode(Document document, string key) : base(document)
        {
        }

        public override void Execute(RenderingContext context)
        {
            context.operation = RenderingOperation.Break;
        }
    }

    public class NewLineNode : TemplateNode
    {
        public NewLineNode(Document document) : base(document)
        {
        }

        public override void Execute(RenderingContext context)
        {
            context.output.AppendLine();
        }
    }

    public class TabNode : TemplateNode
    {
        private int count;

        public TabNode(Document document, string key) : base(document)
        {
            count = int.Parse(key);
        }

        public override void Execute(RenderingContext context)
        {
            context.output.Append(new string('\t', count));
        }
    }    
}
