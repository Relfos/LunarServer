using LunarLabs.WebServer.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security;
using System.Linq;
using System.Text;
using LunarLabs.WebServer.Utils;

namespace LunarLabs.WebServer.Templates
{
    public abstract class TemplateNode
    {
        public TemplateEngine engine { get; internal set; }

        public TemplateNode(TemplateDocument document)
        {
            document.AddNode(this);
        }

        public abstract void Execute(RenderingContext context);
    }

    public class GroupNode : TemplateNode
    {
        public List<TemplateNode> nodes = new List<TemplateNode>();

        public GroupNode(TemplateDocument document) : base(document)
        {

        }

        public override void Execute(RenderingContext context)
        {
            foreach (var node in nodes)
            {
                node.Execute(context);
            }
        }
    }

    public class TextNode : TemplateNode
    {
        public string content;

        public TextNode(TemplateDocument document, string content) : base(document)
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

        public BodyNode(TemplateDocument document) : base(document)
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

    public class IncludeNode : TemplateNode
    {
        private string name;

        public IncludeNode(TemplateDocument document, string name) : base(document)
        {
            this.name = name;
        }

        public override void Execute(RenderingContext context)
        {
            var node = engine.FindTemplate(this.name);

            if (node == null)
            {
                throw new Exception("Could not find include :" +name);
            }

            node.Execute(context);
        }
    }

    public class EvalNode : TemplateNode
    {
        public RenderingKey key;
        public bool escape;

        public EvalNode(TemplateDocument document, string key, bool escape) : base(document)
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

    public class UpperNode : TemplateNode
    {
        public RenderingKey key;

        public UpperNode(TemplateDocument document, string key) : base(document)
        {
            this.key = RenderingKey.Parse(key, RenderingType.String);
        }

        public override void Execute(RenderingContext context)
        {
            var obj = context.EvaluateObject(key);
            if (obj != null)
            {
                var temp = obj.ToString().ToUpper();
                context.output.Append(temp);
            }
        }
    }

    public class LowerNode : TemplateNode
    {
        public RenderingKey key;

        public LowerNode(TemplateDocument document, string key) : base(document)
        {
            this.key = RenderingKey.Parse(key, RenderingType.String);
        }

        public override void Execute(RenderingContext context)
        {
            var obj = context.EvaluateObject(key);
            if (obj != null)
            {
                var temp = obj.ToString().ToLower();
                context.output.Append(temp);
            }
        }
    }

    public class SetNode : TemplateNode
    {
        public string key;

        public SetNode(TemplateDocument document, string key) : base(document)
        {
            this.key = key;
        }

        public override void Execute(RenderingContext context)
        {
            context.Set(key, true);
        }
    }

    public class UrlEncodeNode : TemplateNode
    {
        public RenderingKey key;

        public UrlEncodeNode(TemplateDocument document, string key) : base(document)
        {
            this.key = RenderingKey.Parse(key, RenderingType.String);
        }

        public override void Execute(RenderingContext context)
        {
            var obj = context.EvaluateObject(key);
            if (obj != null)
            {
                var temp = obj.ToString();
                temp = StringUtils.UrlEncode(temp);         
                context.output.Append(temp);
            }
        }
    }

    public class IfNode : TemplateNode
    {
        public RenderingKey condition;
        public TemplateNode trueNode;
        public TemplateNode falseNode;

        public IfNode(TemplateDocument document, string condition) : base(document)
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

        public EachNode(TemplateDocument document, string collection) : base(document)
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
                    /*context.Set("index", index);
                    context.Set("first", index == 0);
                    context.Set("last", index == last);*/
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
                /*context.Set("index", 0);
                context.Set("first", true);
                context.Set("last", false);*/
                inner.Execute(context);
                context.DataStack.RemoveAt(context.DataStack.Count - 1);
            }
        }
    }

    public class CacheNode : TemplateNode
    {
        public string dependencies;
        public TemplateNode body;

        public CacheNode(TemplateDocument document, string dependencies) : base(document)
        {
            this.dependencies = dependencies;
        }

        public override void Execute(RenderingContext context)
        {
            if (body == null)
            {
                throw new Exception("Missing body branch in template node");
            }

            var dependency = TemplateDependency.FindDependency(dependencies);
            //if (dependency != null)
            {
                body.Execute(context);
            }
        }
    }

    public class BreakNode : TemplateNode
    {
        public BreakNode(TemplateDocument document, string key) : base(document)
        {
        }

        public override void Execute(RenderingContext context)
        {
            context.operation = RenderingOperation.Break;
        }
    }


}
