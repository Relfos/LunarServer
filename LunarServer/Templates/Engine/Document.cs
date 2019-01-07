using System;
using System.Collections.Generic;
using static LunarLabs.Templates.Compiler;

namespace LunarLabs.Templates
{
    public class Document
    {
        public TemplateNode Root { get; private set; }

        private List<TemplateNode> _nodes = new List<TemplateNode>();
        public IEnumerable<TemplateNode> Nodes => _nodes;

        public void AddNode(TemplateNode node)
        {
            if (Root == null)
            {
                this.Root = node;
            }

            _nodes.Add(node);
        }

        public void Execute(RenderingContext context)
        {
            this.Root.Execute(context);
        }

        public TemplateNode CompileNode(ParseNode node, Compiler compiler)
        {
            switch (node.tag)
            {
                case "else":
                case "group":
                    {
                        if (node.nodes.Count == 1)
                        {
                            return CompileNode(node.nodes[0], compiler);
                        }

                        var result = new GroupNode(this);
                        foreach (var child in node.nodes)
                        {
                            var temp = CompileNode(child, compiler);
                            result.Nodes.Add(temp);
                        }

                        return result;
                    }

                case "if":
                    {
                        var result = new IfNode(this, node.content);
                        result.trueNode = CompileNode(node.nodes[0], compiler);
                        result.falseNode = node.nodes.Count > 1 ? CompileNode(node.nodes[1], compiler) : null;
                        return result;
                    }

                case "text":
                    return new TextNode(this, node.content);

                case "eval":
                    {
                        var key = node.content;
                        var escape = !key.StartsWith("{");
                        if (!escape)
                        {
                            key = key.Substring(1);
                        }
                        return new EvalNode(this, key, escape);
                    }

                case "each":
                    {
                        var result = new EachNode(this, node.content);
                        result.inner = CompileNode(node.nodes[0], compiler);
                        return result;
                    }

                /*case "cache":
                    {
                        var inner = new GroupNode(this);
                        foreach (var child in node.nodes)
                        {
                            var temp = CompileNode(child);
                            inner.nodes.Add(temp);
                        }

                        return new CacheNode(this, node.content);
                    }*/

                default:
                    {
                        var customTag = compiler.GetCustomTag(node.tag);
                        if (customTag != null)
                        {
                            var result = customTag(this, node.content);
                            return result;
                        }

                        throw new Exception("Can not compile node of type " + node.tag);
                    }
            }
        }
    }
}
