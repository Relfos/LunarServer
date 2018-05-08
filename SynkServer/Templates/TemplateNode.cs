using SynkServer.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security;
using System.Linq;
using System.Text;
using SynkServer.Utils;

namespace SynkServer.Templates
{
    public abstract class TemplateNode
    {
        public TemplateEngine engine { get; internal set; }

        public TemplateNode(TemplateDocument document)
        {
            document.AddNode(this);
        }

        public abstract void Execute(Queue<TemplateDocument> queue, object context, object pointer, StringBuilder output);
    }

    public class GroupNode : TemplateNode
    {
        public List<TemplateNode> nodes = new List<TemplateNode>();

        public GroupNode(TemplateDocument document) : base(document)
        {

        }

        public override void Execute(Queue<TemplateDocument> queue, object context, object pointer, StringBuilder output)
        {
            foreach (var node in nodes)
            {
                node.Execute(queue, context, pointer, output);
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

        public override void Execute(Queue<TemplateDocument> queue, object context, object pointer, StringBuilder output)
        {
            output.Append(content);
        }
    }

    public class BodyNode : TemplateNode
    {

        public BodyNode(TemplateDocument document) : base(document)
        {

        }

        public override void Execute(Queue<TemplateDocument> queue, object context, object pointer, StringBuilder output)
        {
            var next = queue.Dequeue();
            next.Execute(queue, context, pointer, output);
        }
    }

    public class IncludeNode : TemplateNode
    {
        private string name;

        public IncludeNode(TemplateDocument document, string name) : base(document)
        {
            this.name = name;
        }

        public override void Execute(Queue<TemplateDocument> queue, object context, object pointer, StringBuilder output)
        {
            var node = engine.FindTemplate(this.name);

            if (node == null)
            {
                throw new Exception("Could not find include :" +name);
            }

            node.Execute(queue, context, pointer, output);
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

        public override void Execute(Queue<TemplateDocument> queue, object context, object pointer, StringBuilder output)
        {
            var obj = TemplateEngine.EvaluateObject(context, pointer, key);

            if (obj == null && context != pointer)
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

                output.Append(temp);
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

        public override void Execute(Queue<TemplateDocument> queue, object context, object pointer, StringBuilder output)
        {
            var obj = TemplateEngine.EvaluateObject(context, pointer, key);
            if (obj != null)
            {
                var temp = obj.ToString().ToUpper();
                output.Append(temp);
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

        public override void Execute(Queue<TemplateDocument> queue, object context, object pointer, StringBuilder output)
        {
            var obj = TemplateEngine.EvaluateObject(context, pointer, key);
            if (obj != null)
            {
                var temp = obj.ToString().ToLower();
                output.Append(temp);
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

        public override void Execute(Queue<TemplateDocument> queue, object context, object pointer, StringBuilder output)
        {
            var dic = context as Dictionary<string, object>;
            if (dic != null)
            {
                dic[key] = true;
            }
        }
    }

    public class EncodeNode : TemplateNode
    {
        public string key;

        public EncodeNode(TemplateDocument document, string key) : base(document)
        {
            this.key = key;
        }

        public override void Execute(Queue<TemplateDocument> queue, object context, object pointer, StringBuilder output)
        {
            var obj = TemplateEngine.EvaluateObject(context, pointer, key);
            if (obj != null)
            {
                var temp = obj.ToString();
                temp = StringUtils.UrlEncode(temp);         
                output.Append(temp);
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

        public override void Execute(Queue<TemplateDocument> queue, object context, object pointer, StringBuilder output)
        {
            if (this.trueNode == null)
            {
                throw new Exception("Missing true branch in If node");
            }

            var result = TemplateEngine.EvaluateObject(context, pointer, condition);

            var isFalse = result == null || result.Equals(false) || ((result is string) && ((string)result).Length == 0);

            if (!isFalse && result is IEnumerable)
            {
                isFalse = !((IEnumerable)result).Any();
            }

            if (isFalse)
            {
                if (falseNode != null)
                {
                    falseNode.Execute(queue, context, pointer, output);
                }

                return;
            }

            trueNode.Execute(queue, context, pointer, output);
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

        public override void Execute(Queue<TemplateDocument> queue, object context, object pointer, StringBuilder output)
        {
            if (inner == null)
            {
                throw new Exception("Missing inner branch in Each node");
            }

            var obj = TemplateEngine.EvaluateObject(context, pointer, collection);

            if (obj == null)
            {
                return;
            }

            var dic = (Dictionary<string, object>)context;

            var list = obj as IEnumerable;

            if (list != null)
            {
                int index = 0;
                int last = list.Count() - 1;
                foreach (var item in list)
                {
                    dic["@index"] = index;
                    dic["@first"] = index == 0;
                    dic["@last"] = index == last;
                    inner.Execute(queue, context, item, output);
                    index++;
                }
            }
            else
            {
                dic["@index"] = 0;
                dic["@first"] = true;
                dic["@last"] = true;
                inner.Execute(queue, context, obj, output);
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

        public override void Execute(Queue<TemplateDocument> queue, object context, object pointer, StringBuilder output)
        {
            if (body == null)
            {
                throw new Exception("Missing body branch in template node");
            }

            var dependency = TemplateDependency.FindDependency(dependencies);
            //if (dependency != null)
            {
                body.Execute(queue, context, pointer, output);
            }
        }
    }

}
