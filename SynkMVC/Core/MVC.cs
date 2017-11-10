using SynkMVC.Model;
using SynkServer;
using SynkServer.Core;
using SynkServer.HTTP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SynkMVC
{
    public enum PermissonMode
    {
        Hidden,
        Readable,
        Writable
    }

    public struct EntityCacheEntry
    {
        public string className;
        public long id;

        public EntityCacheEntry(string className, long id)
        {
            this.id = id;
            this.className = className;
        }

        public override int GetHashCode()
        {
            return className.GetHashCode() ^ id.GetHashCode();
        }
    }

    public struct ContentCacheEntry
    {
        public byte[] content;
        public HashSet<string> dependencies;
    }

    public class MVC: SitePlugin
    {
        public  Dictionary<string, Module> modules { get; private set; }
        public Dictionary<string, Dictionary<string, string>> languages { get; private set; }

        public Config config { get; private set; }

        public Logger log { get; private set; }

        private bool initialized = false;

        public string siteName { get; private set; }
        public string localPath { get; private set; }

        public MVC(string siteName, string folderName, Logger log)
        {
            this.log = log;
            this.siteName = siteName;
            this.localPath = folderName;
            this.entityCache = new Dictionary<EntityCacheEntry, Entity>();

            this.modules = new Dictionary<string, Module>();

            string entryName = folderName.ToLower() + ".";

            string builtinName = (typeof(MVC).FullName.Split('.')[0] + ".Modules").ToLower();

            Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in asms)
            {
                foreach (Type type in assembly.GetTypes().Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(Module))))
                {
                    if (type.FullName.ToLower().StartsWith(entryName) || type.FullName.ToLower().StartsWith(builtinName))
                    {
                        var module = (Module)Activator.CreateInstance(type);
                        module.InitRoutes(this);
                        this.modules[module.name] = module;
                    }
                }
            }

            this.languages = new Dictionary<string, Dictionary<string, string>>();

            var localPath = GetFullPath("language");
            string[] files = null;

            try
            {
                files = Directory.GetFiles(localPath);
            }
            catch (IOException e)
            {
                this.log.Error(e.Message);
                return;
            }

            char[] sep = new char[1];
            sep[0] = ',';
            foreach (var file in files)
            {
                var lines = System.IO.File.ReadAllLines(file);
                var dic = new Dictionary<string, string>();
                foreach (var line in lines)
                {
                    string[] s = line.Split(sep, 2);
                    if (s.Length < 2)
                    {
                        continue;
                    }
                    string key = s[0];
                    string val = s[1];
                    dic[key] = val;
                }

                var lang = Path.GetFileNameWithoutExtension(file);
                this.languages[lang] = dic;
            }

            this.config = new Config();
            this.config.Load(this);
        }

        public override bool Install(Site site, string path)
        {
            try
            {
                this.site = site;

                Type siteType = null;
                var typeName = localPath + "Site";

                var assembly = Assembly.GetCallingAssembly();
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (type.Name.Equals(typeName))
                    {
                        siteType = type;
                        break;
                    }
                }

                if (siteType == null)
                {
                    log.Error($"Could not load MVC site {siteName} because the class {typeName} was not found.");
                    return false;
                }

                log.Info($"Started {siteName} in folder {localPath}.");

                site.Get(Combine(path, "/"), (req) => { return new SynkContext(this, req); });
                site.Get(Combine(path, "{module}"), (req) => { return new SynkContext(this, req); });
                site.Get(Combine(path, "{module}/{entity}"), (req) => { return new SynkContext(this, req, new Dictionary<string, string> { { "action", " detail" } }); });
                site.Get(Combine(path, "api/{entity}/{action}"), (req) => { return new SynkContext(this, req, new Dictionary<string, string> { { "module", "api" } }); });
                site.Get(Combine(path, "api/{entity}/{action}/{id}"), (req) => { return new SynkContext(this, req, new Dictionary<string, string> { { "module", "api" } }); });

                return true;
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return false;
            }
        }

        #region  COUNTRY LIST
        private static string[] countryList =
        {
        "AF", "ZA", "AL", "DE", "AD", "AO", "AI", "AQ", "AG", "SA", "DZ", "AR", "AM", "AW", "AU", "AT", "AZ", "BS", "BH", "BD", "BB", "BY", "BE", "BZ", "BJ", "BM",
        "BO", "BA", "BW", "BR", "BN", "BG", "BF", "BI", "BT", "CV", "CM", "KH", "CA", "IC", "KZ", "EA", "TD", "CL", "CN", "CY", "SG", "CO", "KM", "CG", "KP", "KR",
        "CI", "CR", "HR", "CU", "CW", "DG", "DK", "DJ", "DM", "EG", "SV", "AE", "EC", "ER", "SK", "SI", "ES", "US", "EE", "ET", "FJ", "PH", "FI", "FR", "GA", "GM",
        "GH", "GE", "GI", "GB", "GD", "GR", "GL", "GP", "GU", "GT", "GG", "GY", "GF", "GN", "GQ", "GW", "HT", "NL", "HN", "HK", "HU", "YE", "BV", "AC", "CP", "IM",
        "CX", "PN", "RE", "AX", "KY", "CC", "CK", "FO", "GS", "HM", "FK", "MP", "MH", "UM", "NF", "SB", "SC", "TK", "TC", "VI", "VG", "IN", "ID", "IR", "IQ", "IE",
        "IS", "IL", "IT", "JM", "JP", "JE", "JO", "KI", "XK", "KW", "LA", "LS", "LV", "LB", "LR", "LY", "LI", "LT", "LU", "MO", "MK", "MG", "MY", "MW", "MV", "ML",
        "MT", "MA", "MQ", "MU", "MR", "YT", "MX", "FM", "MZ", "MD", "MC", "MN", "ME", "MS", "MM", "NA", "NR", "NP", "NI", "NE", "NG", "NU", "NO", "NC", "NZ", "OM",
        "BQ", "PW", "PS", "PA", "PG", "PK", "PY", "PE", "PF", "PL", "PR", "PT", "QA", "KE", "KG", "CF", "CD", "DO", "CZ", "RO", "RW", "RU", "EH", "PM", "AS", "WS",
        "SM", "SH", "LC", "BL", "KN", "MF", "SX", "ST", "VC", "SN", "SL", "RS", "SY", "SO", "LK", "SZ", "SD", "SS", "SE", "CH", "SR", "SJ", "TJ", "TH", "TW", "TZ",
        "IO", "TF", "TL", "TG", "TO", "TT", "TA", "TN", "TM", "TR", "TV", "UA", "UG", "UY", "UZ", "VU", "VA", "VE", "VN", "WF", "ZM", "ZW"
        };

        #endregion

        public virtual void ProcessEntitySchema(Entity entity)
        {

        }

        public virtual void GrabContext(SynkContext context)
        {
            if (!initialized)
            {
                initialized = true;
                log.Info("Initializing " + this.localPath + " site");

                var assembly = this.GetType().Assembly;
                var types = assembly.GetTypes();
                var entities = new List<Entity>();

                entities.Add(new Model.File());
                entities.Add(new Model.Enum());
                entities.Add(new Model.User());

                foreach (var type in types)
                {
                    if (type.IsSubclassOf(typeof(Entity)))
                    {
                        var entity = (Entity)Activator.CreateInstance(type);
                        entities.Add(entity);
                    }
                }

                foreach (var entity in entities)
                {
                    entity.InitFromContext(context);
                    if (GenerateTableForEntity(context, entity))
                    {
                        log.Info("Generated table: " + entity.tableName);
                    }
                }

                var userTotal = context.database.GetEntityCount<User>();
                if (userTotal == 0)
                {
                    var user = context.database.CreateEntity<User>();
                    user.InitFromContext(context);
                    context.config.InitFirstUser(context, user);

                    user.Save(context);

                    bool instanced = config.GetFieldBool("instanced");
                    if (instanced)
                    {
                        context.database.createDatabase(user.GetFieldValue("database"));
                    }
                }


                context.CreateEnum("country", countryList);
                //this.createEnum("product_type", array('product', 'service', 'other'));
            }
        }

        private bool GenerateTableForEntity(SynkContext context, Entity entity)
        {
            if (entity == null || !entity.isWritable)
            {
                return false;
            }

            var dbFields = new Dictionary<string, string>();
            foreach (var field in entity.fields)
            {
                if (!field.writable)
                {
                    continue;
                }

                var fieldType = field.dbType;

                var fieldName = field.name;

                entity.SetFieldValue(fieldName, field.GetDefaultValue(context));

                //$this->$fieldName = $field->defaultValue;

                dbFields[fieldName] = fieldType;
            }

            return context.database.createTable(entity.dbName, entity.tableName, dbFields);
        }

        public string GetFullPath(string localPath)
        {
            var otherSep = Path.DirectorySeparatorChar == '/' ? '\\' : '/';
            localPath = localPath.Replace(otherSep, Path.DirectorySeparatorChar);
            var globalPath = Path.Combine(site.filePath, this.localPath);

            if (localPath.StartsWith(""+Path.DirectorySeparatorChar))
            {
                localPath = localPath.Substring(1);
            }

            return Path.Combine(globalPath, localPath);
        }

        #region CACHE

        public Dictionary<EntityCacheEntry, Entity> entityCache { get; private set; }

        private Dictionary<string, ContentCacheEntry> _cache = new Dictionary<string, ContentCacheEntry>();

        private string GetCacheKey(HTTPRequest request)
        {
            return request.url;
        }

        public void SetCache(HTTPRequest request, byte[] data, HashSet<string> dependencies)
        {
            var entry = new ContentCacheEntry();
            entry.dependencies = dependencies;
            entry.content = data;
            var key = GetCacheKey(request);
            _cache[key] = entry;
        }

        public void InvalidateCache(string dependency)
        {
            List<string> removals = null;

            foreach (var entry in _cache)
            {
                if (entry.Value.dependencies != null && entry.Value.dependencies.Contains(dependency))
                {
                    if (removals == null)
                    {
                        removals = new List<string>();
                    }

                    removals.Add(entry.Key);
                }
            }

            if (removals != null)
            {
                foreach (var entry in removals)
                {
                    _cache.Remove(entry);
                }
            }
        }

        public byte[] GetCache(HTTPRequest request)
        {
            var key = GetCacheKey(request);
            if (_cache.ContainsKey(key))
            {
                return _cache[key].content;
            }

            return null;
        }
        #endregion
    }
}
