using LunarLabs.Templates;
using LunarLabs.WebServer.Core;
using LunarLabs.WebServer.HTTP;
using LunarLabs.WebServer.Minifiers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LunarLabs.WebServer.Templates
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

    public class IncludeNode : TemplateNode
    {
        public readonly TemplateEngine engine;
        private string name;

        public IncludeNode(Document document, string name, TemplateEngine engine) : base(document)
        {
            this.name = name;
            this.engine = engine;
        }

        public override void Execute(RenderingContext context)
        {
            var node = engine.FindTemplate(this.name);

            if (node == null)
            {
                throw new Exception("Could not find include :" + name);
            }

            node.Execute(context);
        }
    }

    public class UrlEncodeNode : TemplateNode
    {
        public RenderingKey key;

        public UrlEncodeNode(Document document, string key) : base(document)
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

    public class CacheNode : TemplateNode
    {
        public string dependencies;
        public TemplateNode body;

        public CacheNode(Document document, string dependencies) : base(document)
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

    public class TemplateEngine
    {
        private struct CacheEntry
        {
            public Document document;
            public DateTime lastModified;            
        }

        private Dictionary<string, CacheEntry> _cache = new Dictionary<string, CacheEntry>();

        public string filePath { get; private set; }

        public Func<string, Document> On404;

        public readonly HTTPServer Server;

        public readonly Compiler Compiler;

        public TemplateEngine(HTTPServer server, string filePath)
        {
            this.Server = server;
            this.Compiler = new Compiler();

            if (filePath == null)
            {
                this.filePath = Server.Settings.Path;
            }
            else
            {
                if (!filePath.EndsWith("/"))
                {
                    filePath += "/";
                }
                this.filePath = Server.Settings.Path + filePath;
            }

            this.On404 = (name) =>
            {
                var doc = new Document();
                doc.AddNode(new TextNode(doc, "error 404: \""+name+"\" view not found"));
                return doc;
            };

            Compiler.RegisterCaseTags();
            Compiler.RegisterFormatTags();
            Compiler.RegisterDateTags();

            Compiler.RegisterTag("include", (doc, key) => new IncludeNode(doc, key, this));
            Compiler.RegisterTag("cache", (doc, key) => new CacheNode(doc, "")); // TODO fixme
            Compiler.RegisterTag("url-encode", (doc, key) => new UrlEncodeNode(doc, key));

            Compiler.RegisterTag("localize", (doc, key) => new LocalizationNode(doc, key, this));

            Compiler.RegisterTag("javascript", (doc, key) => new AssetNode(doc, key, "js", this));
            Compiler.RegisterTag("css", (doc, key) => new AssetNode(doc, key, "css", this));
            Compiler.RegisterTag("store", (doc, key) => new StoreNode(doc, key, this));
        }

        public Document FindTemplate(string name)
        {
            lock (_cache)
            {
                DateTime lastMod;

                var fileName = filePath + name + ".html";
                if (!File.Exists(fileName))
                {
                    if (_cache.ContainsKey(name))
                    {
                        return _cache[name].document;
                    }

                    return this.On404(name);
                }

                lastMod = File.GetLastWriteTime(fileName);

                if (_cache.ContainsKey(name))
                {
                    var entry =_cache[name];

                    if (lastMod == entry.lastModified)
                    {
                        return entry.document;
                    }
                }

                string content;

                content = File.ReadAllText(fileName);

                if (Server.Settings.Environment == ServerEnvironment.Prod)
                {
                    content = HTMLMinifier.Compress(content);
                }

                var doc = Compiler.CompileTemplate(content); 

                _cache[name] = new CacheEntry() { document = doc, lastModified = lastMod};

                return doc;
            }
        }

        public void RegisterTemplate(string name, string content)
        {
            lock (_cache)
            {
                var doc = Compiler.CompileTemplate(content);
                _cache[name] = new CacheEntry() { document = doc, lastModified = DateTime.Now };
            }
        }

        public string RenderDocuments(object data, Queue<Document> documents)
        {
            var next = documents.Dequeue();

            var context = new RenderingContext();
            context.DataRoot = data;
            context.DataStack = new List<object>();
            context.DataStack.Add(data);
            context.queue = documents;
            context.output = new StringBuilder();
            next.Execute(context);

            var html = context.output.ToString();

            return html;
        }

        public string Render(object data, params string[] templateList)
        {
            //var startTime = Environment.TickCount;

            var queue = new Queue<Document>();

            foreach (var templateName in templateList)
            {
                var template = FindTemplate(templateName);
                queue.Enqueue(template);
            }

            var html = RenderDocuments(data, queue);

            /*var endTime = Environment.TickCount;
            var renderDuration = endTime - startTime;
            Console.WriteLine($"RENDERED IN {renderDuration} ms");
            */

            return html;
        }
    }
}
