using Mustache;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security;

namespace SynkServer.Core
{
    public class TemplateEngine
    {
        private FormatCompiler compiler;

        public TemplateEngine(FormatCompiler compiler)
        {
            this.compiler = compiler;
            compiler.RemoveNewLines = false;
            compiler.AreExtensionTagsAllowed = true;
        }

        private static void EscapeInvalidHtml(object sender, TagFormattedEventArgs e)
        {
            if (e.IsExtension)
            {
                // Do not escape text within triple curly braces
                return;
            }
            e.Substitute = SecurityElement.Escape(e.Substitute);
        }

        private static object EvaluateObject(object context, object obj, string key)
        {
            switch (key)
            {
                case "true": return true;
                case "false": return false;
            }

            int intVal;
            if (int.TryParse(key, out intVal))
            {
                return intVal;
            }

            if (key.Contains("_equals_"))
            {
                key = key.Replace("_equals_", "=");
                var temp = key.Split(new char[] { '=' }, 2);
                var leftSide = EvaluateObject(context, obj, temp[0]);
                var rightSide = EvaluateObject(context, obj, temp[1]);

                if (leftSide == null && rightSide == null)
                {
                    return true;
                }

                if ((leftSide == null && rightSide != null) || (leftSide != null && rightSide == null))
                {
                    return false;
                }

                return leftSide.ToString().Equals(rightSide.ToString());
            }

            if (key.Contains("."))
            {
                var temp = key.Split(new char[] { '.' }, 2);
                var objName = temp[0];
                var fieldName = temp[1];

                obj = objName.Equals("this") ?obj: EvaluateObject(context, obj, objName);

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
                return field.GetValue(obj);
            }

            var prop = type.GetProperty(key);
            if (prop != null)
            {
                return prop.GetValue(obj);
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

        public string Render(Site site, Dictionary<string, object> context, string[] templateList)
        {
            string layoutTemplate = "";

            var total = templateList.Length;
            for (var i = total - 1; i >= 0; i--)
            {
                var templateName = templateList[i];

                var localPath = Path.Combine("views", templateName + ".html");
                var fileName = localPath; // site.GetFullPath(localPath);

                string body;

                if (System.IO.File.Exists(fileName))
                {
                    body = System.IO.File.ReadAllText(fileName);
                }
                else
                {
                    var error = "Error loading view '" + templateName + "', the file was not found!";
                    localPath = Path.Combine("views", "404.html");
                    fileName = localPath; // site.GetFullPath(localPath);

                    if (System.IO.File.Exists(fileName))
                    {
                        body = System.IO.File.ReadAllText(fileName);
                    }
                    else
                    {
                        return error;
                    }
                }

                layoutTemplate = body.Replace("$body", layoutTemplate);
            }

            Generator generator; 
            
            lock (compiler) {
                generator = compiler.Compile(layoutTemplate);
            }
            
            generator.TagFormatted += EscapeInvalidHtml;
            generator.KeyNotFound += (sender, e) =>
            {
                e.Handled = true;

                object obj;
                ((Mustache.Scope)sender).TryFind("this", out obj);

                if (obj == null)
                {
                    return;
                }

                e.Substitute = EvaluateObject(context, obj, e.Key);

                return;
            };

            var result = generator.Render(context);
            return result;
        }


    }
}
