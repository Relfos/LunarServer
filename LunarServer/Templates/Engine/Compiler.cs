using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LunarLabs.Templates
{
    public class Compiler
    {
        private Dictionary<string, Func<Document, string, TemplateNode>> _customTags = new Dictionary<string, Func<Document, string, TemplateNode>>();
        public IEnumerable<KeyValuePair<string, Func<Document, string, TemplateNode>>> CustomTags => _customTags;

        public bool ParseNewLines = true;

        public Compiler()
        {
            RegisterStandardTags();
        }

        public void RegisterTag(string name, Func<Document, string, TemplateNode> generator)
        {
            _customTags[name] = generator;
        }

        public enum ParseState
        {
            Default,
            OpenTag,
            CloseTag,
            EndTag,
            String,
        }

        public static void Print(TemplateNode node, int level = 0)
        {
            for (int i = 0; i < level; i++) Console.Write("\t");
            Console.WriteLine(node.GetType().Name);

            if (node is GroupNode)
            {
                var temp = node as GroupNode;

                foreach (var child in temp.Nodes)
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
                return "" + tag + (content != null ? $"=>'{content}'" : "");
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

        public Func<Document, string, TemplateNode> GetCustomTag(string tag)
        {
            if (_customTags.ContainsKey(tag))
            {
                return _customTags[tag];
            }

            return null;
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

                    if (ParseNewLines)
                    {
                        break;
                    }

                    var isWhitespace = c == '\n' || c=='\r'; // char.IsWhiteSpace(c) && c!=' ';

                    switch (state)
                    {
                        case ParseState.String: shouldContinue = false; break;
                        case ParseState.Default: shouldContinue = isWhitespace; break;
                        default: shouldContinue = false; break;
                    }

                } while (shouldContinue && i < code.Length);

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

                                    if (tagName == "parse-lines")
                                    {
                                        this.ParseNewLines = bool.Parse(temp.ToString());
                                    }
                                    else
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

                                if (c == '\'')
                                {
                                    state = ParseState.String;
                                }
                            }

                            break;
                        }

                    case ParseState.String:
                        {
                            temp.Append(c);

                            if (c == '\'')
                            {
                                state = ParseState.OpenTag;
                            }

                            break;
                        }

                    case ParseState.EndTag:
                        {
                            if (c == '}')
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
                                    throw new Exception("Expecting end of " + result.tag + " but got " + key);
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

        private void RegisterStandardTags()
        {
            RegisterTag("body", (doc, key) => new BodyNode(doc));
            RegisterTag("count", (doc, key) => new CountNode(doc, key));
            RegisterTag("set", (doc, key) => new SetNode(doc, key));
            RegisterTag("break", (doc, key) => new BreakNode(doc, key));
            RegisterTag("new-line", (doc, key) => new NewLineNode(doc));
            RegisterTag("tab", (doc, key) => new TabNode(doc, key));
        }

        public void RegisterDateTags()
        {
            RegisterTag("date", (doc, key) => new DateNode(doc, key));
            RegisterTag("span", (doc, key) => new SpanNode(doc, key));
        }

        public void RegisterFormatTags()
        {
            RegisterTag("format-amount", (doc, key) => new NumericFormatNode(doc, key, NumericFormatNode.AmountFormatters));
            RegisterTag("format-size", (doc, key) => new NumericFormatNode(doc, key, NumericFormatNode.SizeFormatters));
        }

        public void RegisterCaseTags()
        {
            var cases = Enum.GetValues(typeof(CaseKind)).Cast<CaseKind>();
            foreach (var val in cases)
            {
                var name = val.ToString().ToLower();
                RegisterTag($"{name}-case", (doc, key) => new CaseNode(doc, key, val));
            }
        }

        public Document CompileTemplate(string code)
        {
            var obj = ParseTemplate(code);

            var doc = new Document();
            var result = doc.CompileNode(obj, this);

            //Print(result);
            return doc;
        }

    }
}
