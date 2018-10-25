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
    public class RenderingContext
    {
        public Queue<TemplateDocument> queue;
        public object DataContext;
        public object pointer;
        public StringBuilder output;
        
        private Dictionary<string, object> variables;

        internal void Set(string key, object val)
        {
            if (variables == null)
            {
                variables = new Dictionary<string, object>();
            }

            variables[key] = val;
        }

        internal object Get(string key)
        {
            if (variables == null)
            {
                return null;
            }

            if (variables.ContainsKey(key))
            {
                return variables[key];
            }

            return null;
        }

        public object EvaluateObject(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            if (key.StartsWith("@"))
            {
                key = key.Substring(1);
                return Get(key);
            }

            return TemplateEngine.EvaluateObject(DataContext, pointer, key);
        }
    }

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
            var next = context.queue.Dequeue();
            next.Execute(context);
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
        public string key;
        public bool escape;

        public EvalNode(TemplateDocument document, string key, bool escape) : base(document)
        {
            this.key = key;
            this.escape = escape;
        }

        public override void Execute(RenderingContext context)
        {
            var obj = context.EvaluateObject(key);

            if (obj == null && context != context.pointer)
            {
                obj = TemplateEngine.EvaluateObject(context, context, key);
            }

            if (obj != null)
            {
                var temp = obj.ToString();

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
        public string key;

        public UpperNode(TemplateDocument document, string key) : base(document)
        {
            this.key = key;
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
        public string key;

        public LowerNode(TemplateDocument document, string key) : base(document)
        {
            this.key = key;
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
        public string key;

        public UrlEncodeNode(TemplateDocument document, string key) : base(document)
        {
            this.key = key;
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
        public string condition;
        public TemplateNode trueNode;
        public TemplateNode falseNode;

        public IfNode(TemplateDocument document, string condition) : base(document)
        {
            this.condition = condition;
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
        public string collection;

        public TemplateNode inner;

        public EachNode(TemplateDocument document, string collection) : base(document)
        {
            this.collection = collection;
        }

        public override void Execute(RenderingContext context)
        {
            if (inner == null)
            {
                throw new Exception("Missing inner branch in Each node");
            }

            var obj = context.EvaluateObject(collection);

            if (obj == null)
            {
                return;
            }

            var list = obj as IEnumerable;

            if (list != null)
            {
                int index = 0;
                int last = list.Count() - 1;
                foreach (var item in list)
                {
                    context.Set("index", index);
                    context.Set("first", index == 0);
                    context.Set("last", index == last);
                    context.pointer = item;
                    inner.Execute(context);
                    index++;
                }
            }
            else
            {
                context.pointer = obj;
                context.Set("index", 0);
                context.Set("first", true);
                context.Set("last", false);
                inner.Execute(context);
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

}
