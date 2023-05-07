using LunarLabs.Templates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LunarLabs.WebServer.Templates
{
    public class Language
    {
        public readonly string code;
        public readonly string name;

        public Language(string code, string name)
        {
            this.code = code;
            this.name = name;
        }
    }

    public class Localization
    {
        private Dictionary<string, string> _entries = new Dictionary<string, string>();

        public string Name { get; private set; }
        public readonly string Location;

        public Localization(string fileName)
        {
            this.Name = Path.GetFileNameWithoutExtension(fileName);
            this.Location = fileName;
            
            var newName = Reload();
            if (!string.IsNullOrEmpty(newName))
            {
                this.Name = newName;
            }

            lastWrite = File.GetLastWriteTimeUtc(Location);
        }

        private string Reload() 
        {
            string result = null;

            _entries.Clear();

            var lines = File.ReadAllLines(Location);
            var ch = new char[] { ',' };
            foreach (var line in lines)
            {
                var temp = line.Split(ch, 2);
                if (temp.Length != 2)
                {
                    continue;
                }

                var key = temp[0];
                var val = temp[1];

                if (key == "language_name")
                {
                    result = val;
                }

                _entries[key] = val;
            }

            lastCheck = DateTime.UtcNow;
            return result;
        }

        public string Localize(string key)
        {
            CheckReload();

            if (_entries.ContainsKey(key))
            {
                return _entries[key];
            }

            return $"??{key}??";
        }

        private DateTime lastCheck;
        private DateTime lastWrite;

        private void CheckReload()
        {
            var now = DateTime.UtcNow;

            var diff = now - lastCheck;

            if (diff.TotalSeconds < 1)
            {
                return;
            }

            var fileAge = File.GetLastWriteTimeUtc(Location);

            if (fileAge >  lastWrite)
            {
                Console.WriteLine("Reloading " + Location);

                Reload();

                lastWrite = fileAge;
            }

            lastCheck = now;
        }
    }

    public static class LocalizationManager
    {
        private static Dictionary<string, Localization> _localizations = new Dictionary<string, Localization>();
        private static Dictionary<string, Language> _languages = new Dictionary<string, Language>();

        public static IEnumerable<Language> Languages => _languages.Values;

        public static Localization LoadLocalization(string code, string fileName)
        {
            if (!File.Exists(fileName))
            {
                return null;
            }

            var localization = new Localization(fileName);
            _localizations[code] = localization;

            _languages[code] = new Language(code, localization.Name);

            return localization;
        }


        public static bool HasLanguage(string code)
        {
            return _languages.ContainsKey(code);
        }

        public static Language GetLanguage(string code)
        {
            if (_languages.ContainsKey(code))
            {
                return _languages[code];
            }

            return null;
        }

        public static string Localize(string language, string key)
        {
            if (_localizations.ContainsKey(language))
            {
                return _localizations[language].Localize(key);
            }

            return $"??{key}!?";
        }
    }

    public class LocalizationNode : TemplateNode
    {
        public static Dictionary<string, List<string>> assetList = new Dictionary<string, List<string>>();

        private string key;

        private TemplateEngine engine;

        private static RenderingKey languageKey = RenderingKey.Parse("current_language", RenderingType.Any);

        public LocalizationNode(Document document, string key, TemplateEngine engine) : base(document)
        {
            this.key = key;
            this.engine = engine;
        }

        public override void Execute(RenderingContext context)
        {
            var language = context.EvaluateObject(languageKey) as Language;
            if (language == null)
            {
                language = LocalizationManager.GetLanguage("en");
                if (language == null)
                {
                    return;
                }
            }

            var text = LocalizationManager.Localize(language.code, this.key);
            context.output.Append(text);
        }
    }
}
