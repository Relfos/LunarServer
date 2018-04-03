using LunarParser;
using SynkServer.Core;
using SynkServer.HTTP;
using SynkServer.Minifiers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SynkServer.Templates
{
    public abstract class TemplateDependency
    {
        public int currentVersion { get; protected set; }

        public TemplateDependency(string name)
        {
            _dependencies[name] = this;
        }

        private static Dictionary<string, TemplateDependency> _dependencies = new Dictionary<string, TemplateDependency>();

        public static TemplateDependency FindDependency(string name)
        {
            if (_dependencies.ContainsKey(name))
            {
                return _dependencies[name];
            }

            return null;
        }
    }

    public class TemplateEngine
    {
        private struct CacheEntry
        {
            public TemplateNode node;
            public DateTime lastModified;            
        }

        private Dictionary<string, CacheEntry> _cache = new Dictionary<string, CacheEntry>();

        public string filePath { get; private set; }

        public Func<string, TemplateNode> On404;

        public TemplateEngine(HTTPServer server, string filePath)
        {
            this.filePath = server.settings.path + filePath;

            if (!this.filePath.EndsWith("/"))
            {
                this.filePath += "/";
            }

            this.On404 = (name) =>
            {
                return new TextNode("404 not found");
            };

            RegisterTag("body", x => new BodyNode());
            RegisterTag("encode", x => new EncodeNode(x));
            RegisterTag("include", x => new IncludeNode(x));
            RegisterTag("date", arg => new DateNode(arg));
            RegisterTag("span", arg => new SpanNode(arg));
            RegisterTag("upper", arg => new UpperNode(arg));
            RegisterTag("lower", arg => new LowerNode(arg));
        }

        public TemplateNode FindTemplate(string name)
        {
            lock (_cache)
            {
                DateTime lastMod;

                var fileName = filePath + name + ".html";
                if (!File.Exists(fileName))
                {
                    if (_cache.ContainsKey(name))
                    {
                        return _cache[name].node;
                    }

                    return this.On404(name);
                }

                lastMod = File.GetLastWriteTime(fileName);

                if (_cache.ContainsKey(name))
                {
                    var entry =_cache[name];

                    if (lastMod == entry.lastModified)
                    {
                        return entry.node;
                    }
                }

                string content;

                content = File.ReadAllText(fileName);

                content = HTMLMinifier.Compress(content);

                var node = CompileTemplate(content); 

                _cache[name] = new CacheEntry() { node = node, lastModified = lastMod};

                return node;
            }
        }

        public static object EvaluateObject(object context, object obj, string key)
        {
            bool negate = false;

            if (key.StartsWith("!"))
            {
                key = key.Substring(1);
                negate = true;
            }

            switch (key)
            {
                case "true": return !negate;
                case "false": return negate;
                case "this": return obj;
            }

            if (key.StartsWith("'") && key.EndsWith("'"))
            {
                return key.Substring(1, key.Length - 2);
            }

            int intVal;
            if (int.TryParse(key, out intVal))
            {
                return intVal;
            }

            if (key.Contains("="))
            {
                var temp = key.Split(new char[] { '=' }, 2);
                var leftSide = EvaluateObject(context, obj, temp[0]);
                var rightSide = EvaluateObject(context, obj, temp[1]);

                bool val;

                if (leftSide == null && rightSide == null)
                {
                    val = true;
                }
                else
                if ((leftSide == null && rightSide != null) || (leftSide != null && rightSide == null))
                {
                    val = false;
                }
                else
                {
                    val = leftSide.ToString().Equals(rightSide.ToString());
                }
                
                if (negate) val = !val;

                return val;
            }

            if (key.Contains("."))
            {
                var temp = key.Split(new char[] { '.' }, 2);
                var objName = temp[0];
                var fieldName = temp[1];

                obj = EvaluateObject(context, obj, objName);

                if (obj != null)
                {
                    return EvaluateObject(context, obj, fieldName);
                }

                return null;
            }

            Type type = obj.GetType();
            var field = type.GetField(key);
            if (field != null)
            {
                var result = field.GetValue(obj);

                if (negate && result is bool)
                {
                    return !((bool)result);
                }

                return result;
            }

            var prop = type.GetProperty(key);
            if (prop != null)
            {
                var result = prop.GetValue(obj);

                if (negate && result is bool)
                {
                    return !((bool)result);
                }

                return result;
            }

            DataNode node = obj as DataNode;
            if (node != null)
            {
                if (node.HasNode(key))
                {
                    var val = node.GetNode(key);
                    if (val != null)
                    {
                        if (val.ChildCount > 0)
                        {
                            return val;
                        }
                        else
                        {
                            return val.Value;
                        }
                    }

                    return null;
                }

                return null;
            }

            IDictionary dict = obj as IDictionary;
            if (dict != null)
            {
                if (dict.Contains(key))
                {
                    obj = dict[key];
                    return obj;
                }

                type = obj.GetType();
                Type valueType = type.GetGenericArguments()[1];
                return valueType.GetDefault();
            }

            ICollection collection;
            try
            {
                collection = (ICollection)obj;
            }
            catch
            {
                collection = null;
            }

            if (collection != null && key.Equals("count"))
            {
                return collection.Count;
            }

            if (obj != context)
            {
                return EvaluateObject(context, context, key);
            }

            return null;
        }

        public void RegisterTemplate(string name, string content)
        {
            lock (_cache)
            {
                var node = CompileTemplate(content);

                _cache[name] = new CacheEntry() { node = node, lastModified = DateTime.Now };
            }
        }

        public string Render(Site site, object context, IEnumerable<string> templateList)
        {
            var queue = new Queue<TemplateNode>();

            foreach (var templateName in templateList)
            {
                var template = FindTemplate(templateName);
                queue.Enqueue(template);
            }

            var output = new StringBuilder();
            var next = queue.Dequeue();
            next.Execute(queue, context, context, output);

            var html = output.ToString();
            return html;
        }

        public enum ParseState
        {
            Default,
            OpenTag,
            CloseTag,
            InsideTag,
            EndTag
        }

        public static void Print(TemplateNode node, int level = 0)
        {
            for (int i = 0; i < level; i++) Console.Write("\t");
            Console.WriteLine(node.GetType().Name);

            if (node is GroupNode)
            {
                var temp = node as GroupNode;

                foreach (var child in temp.nodes)
                {
                    Print(child, level + 1);
                }
            }
            else
            if (node is IfNode)
            {
                var temp = node as IfNode;

                for (int i = 0; i <= level; i++) Console.Write("\t");
                Console.WriteLine(".true");
                Print(temp.trueNode, level + 2);

                if (temp.falseNode != null)
                {
                    for (int i = 0; i <= level; i++) Console.Write("\t");
                    Console.WriteLine(".false");
                    Print(temp.falseNode, level + 2);
                }
            }
            else
            if (node is EachNode)
            {
                var temp = node as EachNode;

                Print(temp.inner, level + 1);
            }
        }

        public static void Print(ParseNode node, int level = 0)
        {
            for (int i = 0; i < level; i++) Console.Write("\t");
            Console.Write(node.tag);
            if (node.content != null)
            {
                Console.Write($"=>'{node.content}'");
            }
            Console.WriteLine();

            foreach (var child in node.nodes)
            {
                Print(child, level + 1);
            }
        }

        public class ParseNode
        {
            public string tag;
            public string content;
            public ParseNode parent;
            public List<ParseNode> nodes = new List<ParseNode>();

            public ParseNode(ParseNode parent)
            {
                this.parent = parent;
            }

            public override string ToString()
            {
                return "" + tag + (content!=null ? $"=>'{content}'" :"");
            }
        }

        public ParseNode ParseTemplate(string code)
        {
            int i = 0;
            var result = new ParseNode(null);
            result.tag = "group";
            ParseTemplate(result, code, ref i);
            return result;
        }

        private static bool TagMismatch(string a, string b)
        {
            if (a == b) return false;

            if (b == "group" && (a == "each" || a == "if"))
            {
                return false;
            }

            return true;
        }

        public void ParseTemplate(ParseNode result, string code, ref int i)
        {
           
            var output = new StringBuilder();
            var state = ParseState.Default;

            char c = '\0';
            char prev;

            var temp = new StringBuilder();
            string tagName = null;

            bool isSpecialTag = false;

            bool isFinished = false;

            while (i < code.Length)
            {
                bool shouldContinue;
                do
                {
                    prev = c;
                    c = code[i];
                    i++;

                    var isWhitespace = char.IsWhiteSpace(c);

                    shouldContinue = false;
                } while (shouldContinue);

                switch (state)
                {
                    case ParseState.Default:
                        {
                            if (c == '{' && c == prev)
                            {
                                output.Length--;

                                if (output.Length != 0)
                                {
                                    var node = new ParseNode(result);
                                    node.tag = "text";
                                    node.content = output.ToString();
                                    result.nodes.Add(node);
                                    output.Length = 0;
                                }

                                state = ParseState.OpenTag;
                                isSpecialTag = false;
                                tagName = null;
                                temp.Length = 0;
                            }
                            else
                            {
                                output.Append(c);
                            }

                            break;
                        }

                    case ParseState.OpenTag:
                        {
                            if (c == '/')
                            {
                                state = ParseState.EndTag;
                                temp.Length = 0;
                            }
                            else
                            if (c == '}')
                            {
                                if (prev == c)
                                {
                                    if (isSpecialTag)
                                    {
                                        tagName = temp.ToString();
                                        temp.Length = 0;
                                    }

                                    if (tagName != null)
                                    {
                                        bool isCustom = false;

                                        switch (tagName)
                                        {
                                            case "else":
                                            case "each":
                                            case "if":
                                            case "cache":
                                                break;

                                            default:
                                                if (!_customTags.ContainsKey(tagName))
                                                {
                                                    throw new Exception("Unknown tag: " + tagName);
                                                }

                                                isCustom = true;
                                                break;
                                        }

                                        var node = new ParseNode(result);
                                        node.tag = tagName;
                                        node.content = temp.ToString();

                                        var aux = result;

                                        if (node.tag == "else")
                                        {
                                            aux = aux.parent;
                                        }

                                        aux.nodes.Add(node);

                                        if (!isCustom)
                                        {
                                            if (node.tag == "if" || node.tag == "each")
                                            {
                                                aux = node;
                                                node = new ParseNode(aux);
                                                node.tag = "group";
                                                aux.nodes.Add(node);
                                            }

                                            ParseTemplate(node, code, ref i);

                                            if (node.tag == "else")
                                            {
                                                return;
                                            }
                                        }

                                    }
                                    else
                                    {
                                        var key = temp.ToString();

                                        bool unscape = key.StartsWith("{");

                                        if (unscape)
                                        {
                                            i++;
                                        }

                                        var node = new ParseNode(result);
                                        node.tag = "eval";
                                        node.content = key;
                                        result.nodes.Add(node);

                                    }

                                    state = ParseState.Default;
                                }
                            }
                            else
                            if (c == ' ' && isSpecialTag)
                            {
                                isSpecialTag = false;
                                tagName = temp.ToString();
                                temp.Length = 0;
                            }
                            else
                            if (c == '#')
                            {
                                isSpecialTag = true;
                            }
                            else
                            {
                                temp.Append(c);
                            }

                            break;
                        }

                    case ParseState.EndTag:
                        {
                            if (c=='}')
                            {
                                string key = temp.ToString();

                                string expected;

                                if (result.tag == "else")
                                {
                                    expected = "if";
                                }
                                else
                                {
                                    expected = result.tag;
                                }

                                if (TagMismatch(key, expected))
                                {
                                    throw new Exception("Expecting end of " + result.tag+" but got "+key);
                                }

                                isFinished = true;
                                state = ParseState.CloseTag;
                            }
                            else
                            {
                                temp.Append(c);
                            }

                            break;
                        }

                    case ParseState.CloseTag:
                        {
                            if (c == '}')
                            {
                                if (c == prev)
                                {
                                    state = ParseState.Default;

                                    if (isFinished)
                                    {
                                        return;
                                    }
                                }
                            }
                            else
                            {
                                throw new Exception("Expected end of tag");
                            }

                            break;
                        }
                }
            }

            if (output.Length > 0)
            {
                var node = new ParseNode(result);
                node.tag = "text";
                node.content = output.ToString();
                result.nodes.Add(node);
                output.Length = 0;
            }
        }

        private TemplateNode CompileNode(ParseNode node)
        {
            switch (node.tag)
            {
                case "else":
                case "group":
                    {
                        if (node.nodes.Count == 1)
                        {
                            return CompileNode(node.nodes[0]);
                        }

                        var result = new GroupNode();
                        foreach (var child in node.nodes)
                        {
                            var temp = CompileNode(child);
                            result.nodes.Add(temp);
                        }

                        return result;
                    }

                case "if":
                    {
                        var trueNode = CompileNode(node.nodes[0]);
                        var falseNode = node.nodes.Count > 1 ? CompileNode(node.nodes[1]) : null;
                        var result = new IfNode(node.content, trueNode, falseNode);
                        return result;
                    }

                case "text":
                    return new TextNode(node.content);

                case "eval":
                    {
                        var key = node.content;
                        var escape = !key.StartsWith("{");
                        if (!escape)
                        {
                            key = key.Substring(1);
                        }
                        return new EvalNode(key, escape);
                    }

                case "each":
                    {
                        var inner = CompileNode(node.nodes[0]);
                        return new EachNode(node.content, inner);
                    }

                case "cache":
                    {
                        var inner = new GroupNode();
                        foreach (var child in node.nodes)
                        {
                            var temp = CompileNode(child);
                            inner.nodes.Add(temp);
                        }
                        
                        return new CacheNode(node.content, inner);
                    }

                default:
                    {
                        if (_customTags.ContainsKey(node.tag))
                        {
                            var generator = _customTags[node.tag];
                            var result = generator(node.content);
                            result.engine = this;
                            return result;
                        }

                        throw new Exception("Can not compile node of type " + node.tag);
                    }
            }
        }

        public TemplateNode CompileTemplate(string code)
        {
            var obj = ParseTemplate(code);

            var result = CompileNode(obj);

            //Print(result);
            return result;
        }

        private Dictionary<string, Func<string, TemplateNode>> _customTags = new Dictionary<string, Func<string, TemplateNode>>();

        public void RegisterTag(string name, Func<string, TemplateNode> generator) 
        {
            _customTags[name] = generator;
        }


    }
}
