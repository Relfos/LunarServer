using Mustache;
using LunarLabs.WebServer.Core;
using LunarLabs.WebMVC.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Text;
using System.Security;
using System.Reflection;
using LunarLabs.WebMVC.Model;
using LunarLabs.WebServer.HTTP;
using LunarParser;

namespace LunarLabs.WebMVC
{
    public class SynkRequestException : Exception
    {

    }

    public class SynkContext
    {
        public MVC site { get; private set; }

        public List<Menu> menus = new List<Menu>();
        public Menu profile;

        public bool hasLogin;
        public bool isDownload;
        public string entityID;
        public string outputTarget;
        public string outputURL;
        public List<string> templateStack = new List<string>();

        public Dictionary<string, string> text = new Dictionary<string, string>();

        public string targetView;

        public User currentUser = null;

        public string error = null;

        private string confirmationText = null;
        private string confirmationToken = null;

        public Module currentModule;
        public Module module { get { return currentModule; } }

        public string action = null;

        public Config config;

        public string language;

        public string dbName;

        public Database database;

        public string warning = "";

        public HTTPRequest request { get; private set; }

        public SynkContext(MVC site, HTTPRequest request, Dictionary<string, string> args = null)
        {
            this.site = site;
            this.request = request;

            if (args!=null)
            {
                foreach (var entry in args) {
                    request.args[entry.Key] = entry.Value;
                }                
            }
        }

        public void Init()
        {
            html.Clear();
            binaryData = null;

            menus.Clear();

            this.error = null;
            this.confirmationText = null;

        }

        public object Get(bool sendContent)
        {
            return Response(sendContent, true);
        }

        public object Post()
        {
            return Response(true, false);
        }

        private object FinishRequest()
        {
            if (request.HasVariable("json") && !this.isDownload)
            {
                var result = DataNode.CreateObject();
                result.AddField("target", this.outputTarget);
                result.AddField("module", this.currentModule.name);
                result.AddField("title", this.currentModule.getTitle(this));
                result.AddField("content", this.html.ToString());

                if (!string.IsNullOrEmpty(this.outputURL))
                {
                    result.AddField("url", this.outputURL);
                }

                if (!string.IsNullOrEmpty(this.error))
                {
                    result.AddField("error", this.error);
                }

                if (!string.IsNullOrEmpty(this.confirmationText))
                {
                    var conf = DataNode.CreateObject("confirm");
                    conf.AddField("text", confirmationText);
                    conf.AddField("token", confirmationToken);
                    result.AddNode(conf);
                }

                return result; 
            }
            else
            {
                return html;
            }
        }

        public bool RequestConfirmation(string text)
        {
            var hash = Math.Abs(text.GetHashCode()).ToString();

            var temp = request.GetVariable("confirm");
            if (!string.IsNullOrEmpty(temp) && temp.Equals(hash))
            {
                return false;
            }

            this.confirmationText = text+"<br>Deseja prosseguir?";
            this.confirmationToken = hash;

            return true;            
        }

        private object Response(bool sendContent, bool fullPage)
        {
            object content;

            try
            {
                this.error = null;

                this.config = site.config;
                //this.config.InitFromContext(this);

                if (fullPage && this.config.GetFieldBool("cache"))
                {
                    content = this.site.GetCache(this.request);
                    if  (content != null)
                    {
                        return content;
                    }
                }

                this.Init(fullPage);

                this.RunController();

                if (this.binaryData != null)
                {
                    return this.binaryData;
                }

                content = FinishRequest();
            }
            catch (SynkRequestException e)
            {
                content = FinishRequest();
            }

            if (fullPage && database!=null)
            {
                var deps = database.dependencies;
                //this.site.SetCache(this.request, content, deps); TODO FIXME
            }

            return content;
        }

        private void Init(bool fullPage)
        {
            hasLogin = this.request.session.Contains("user_id");

            this.outputURL = null;
            this.outputTarget = loadVarFromRequest("target", "main");
            this.action = this.loadVarFromRequest("action", "default");

            this.language = loadVar("language", this.config.GetFieldValue("defaultLanguage"));
            if (site.languages.ContainsKey(this.language))
            {
                this.text = site.languages[this.language];
            }
            else
            {   // find any language available
                foreach (var entry in site.languages)
                {
                    this.text = entry.Value;
                    break;
                }
            }


            /*$sqlPlugin = $this->config->sqlPlugin;

		    $pluginPath = 'plugins/database/'.$sqlPlugin.'.php';
            if (!file_exists($pluginPath))
            {
                echo 'Missing database plugin: '.$sqlPlugin;
                die();
            }
            require_once($pluginPath);

		    $dbClassName = $this->config->sqlPlugin.'Plugin';
		    $this->database = new $dbClassName($this);*/
            this.database = new MySQLDatabase(this);

            this.site.GrabContext(this);

            if (this.hasLogin)
            {
                this.dbName = loadVar("user_db", "");

                long user_id;

                long.TryParse(this.request.session.Get<string>("user_id"), out user_id);
                this.currentUser = this.database.FetchEntityByID<User>(user_id);
            }
            else
            {
                this.dbName = this.config.GetFieldValue("database");
            }

            this.database.prepare();

            if (this.database.failed)
            {
                this.hasLogin = false;
                die("Database error..");
                //this.ChangeModule("settings");
                return;
            }

            var defaultModule = config.GetFieldValue("defaultModule");
            var moduleName = this.loadVarFromRequest("module", defaultModule);
            this.ChangeModule(moduleName);

            if (this.hasLogin && this.currentModule!=null && this.currentModule.name.Equals("auth") && !this.action.Equals("logout"))
            {
                ChangeModule(defaultModule);
                this.outputTarget = "main";
                 fullPage = true;
            }

            if (fullPage)
            {
                this.ForceFullPage();
            }

            if (this.currentModule != null)
            {
                if (!this.currentModule.CheckPermissions(this, this.currentUser, this.action))
                {
                    //echo 'oll'; die();
                    this.ChangeModule("auth");
                    this.ChangeAction("forbidden");
                    this.Reload();
                }

                this.currentModule.title = this.currentModule.getTitle(this);
            }

        }

        public bool WaitingForConfirmation()
        {
            return !string.IsNullOrEmpty(confirmationText);
        }

        private byte[] binaryData;
        private StringBuilder html = new StringBuilder();

        public void Echo(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return;
            }

            html.Append(s);
        }

        public void die(string s = null)
        {
            Echo(s);
            throw new SynkRequestException();
        }

        public void ForceFullPage()
        {
            this.PushTemplate("header");
            this.PushTemplate("body");
            this.isReloading = true;
        }

        public void RunController()
        {
            if (this.currentModule == null)
            {
                return;
            }

            var method = this.currentModule.GetType().GetMethod("On" + this.action.FirstLetterToUpper());

            if (method == null)
            {
                this.currentModule.OnInvalidAction(this, this.action);
                return;
            }

            try
            {
                method.Invoke(this.currentModule, new object[] { this });

                if (!this.action.Equals("default"))
                {
                    this.outputURL = this.currentModule.GetActionURL(this.action);
                }
            }
            catch (TargetInvocationException e)
            {
                Console.WriteLine(e.InnerException.StackTrace);                
                throw e.InnerException;
            }
        }

        public void CreateEnum(string name, IEnumerable<string> values)
        {
            var sb = new StringBuilder();
            foreach (string val in values)
            {
                if (sb.Length > 0)
                {
                    sb.Append('|');
                }

                sb.Append(val);
            }

            var cond = Condition.Equal("name", name);
            var entry = database.FetchEntity<SynkMVC.Model.Enum>(cond);
            entry.SetFieldValue("name", name);
            entry.SetFieldValue("values", sb.ToString());
            entry.Save(this);
        }

        public List<string> FetchEnum(string name)
        {
            var cond = Condition.Equal("name", name);
            var entry = this.database.FetchEntity<SynkMVC.Model.Enum>(cond);

            var result = new List<string>();
            if (entry.exists)
            {
                var temp = entry.GetFieldValue("values");
                string[] values = temp.Split('|');
                foreach (var s in values)
                {
                    result.Add(s);
                }
            }
            return result;
        }

        public void kill(string error)
        {
            this.error = error;
            this.PushTemplate("404");
            this.Render();
            //$this->terminate();		die();
        }

        public void ChangeModule(string moduleName)
        {
            if (site.modules.ContainsKey(moduleName))
            {
                this.currentModule = site.modules[moduleName];
            }
            else
            {
                this.kill("Could not load module: " + moduleName);
            }
        }


        public void ChangeAction(string action)
        {
            this.action = action;
        }

        public void LogIn(long user_id, string user_db)
        {
            request.session.Set("user_id", user_id.ToString());
            request.session.Set("user_db", user_db);
            this.hasLogin = true;
            this.currentUser = this.database.FetchEntityByID<User>(user_id);

            this.site.GrabContext(this);
        }

        public void LogOut()
        {
            request.session.Remove("user_id");
            request.session.Remove("user_db");
            request.session.Destroy();
            this.hasLogin = false;
        }

        private bool isReloading = false;
        private object requset;

        public void Reload()
        {
            if (this.isReloading)
            {
                return;
            }

            this.isReloading = true;
            this.outputTarget = "body_content";
            this.PushTemplate("body");
        }

        public void PushTemplate(string fileName)
        {
            this.templateStack.Add(fileName);
        }

        public string loadVar(string name, string defaultValue)
        {
            if (request.HasVariable(name))
            {
                return request.GetVariable(name);
            }

            if (request.session.Contains(name))
            {
                return request.session.Get<string>(name);
            }

            return defaultValue;
        }

        public string loadVarFromSession(string name, string defaultValue)
        {
            if (request.session.Contains(name))
            {
                return request.session.Get<string>(name);
            }

            return defaultValue;
        }

        public string loadVarFromRequest(string name, string defaultValue = "")
        {
            if (request.HasVariable(name))
            {
                return request.GetVariable(name);
            }

            return defaultValue;
        }

        public void WriteLog(string text)
        {
            this.site.log.Info(text);
            /*if (!is_null($this->config->logFile))
		    {
    			file_put_contents($this->config->logFile, "$text\n", FILE_APPEND | LOCK_EX);
	    	}*/
        }

        public void Render()
        {
            string layoutTemplate = "";

            if (this.currentModule != null)
            {
                this.currentModule.beforeRender(this);
            }

            var total = this.templateStack.Count;
            for (var i = total - 1; i >= 0; i--)
            {
                var templateName = templateStack[i];

                var localPath = Path.Combine("views", templateName + ".html");
                var fileName = site.GetFullPath(localPath);

                string body;

                if (System.IO.File.Exists(fileName))
                {
                    body = System.IO.File.ReadAllText(fileName);
                }
                else
                {
                    this.error = "Error loading view '" + templateName + "', the file was not found!";
                    localPath = Path.Combine("views", "404.html");
                    fileName = site.GetFullPath(localPath);

                    if (System.IO.File.Exists(fileName))
                    {
                        body = System.IO.File.ReadAllText(fileName);
                    }
                    else
                    {
                        this.Echo(this.error);
                        return;
                    }
                }

                layoutTemplate = body.Replace("$body", layoutTemplate);
            }

            if (this.currentModule != null)
            {
                this.currentModule.afterRender(this, layoutTemplate);
            }

            var compiler = new FormatCompiler();
            compiler.RemoveNewLines = false;
            compiler.AreExtensionTagsAllowed = true;
            Generator generator = compiler.Compile(layoutTemplate);
            generator.TagFormatted += escapeInvalidHtml;
            generator.KeyNotFound += (sender, e) =>
            {
                e.Handled = true;

                object obj;
                ((Mustache.Scope)sender).TryFind("this", out obj);

                var entity = obj as Entity;
                if (entity != null)
                {
                    e.Substitute = entity.GetFieldValue(e.Key);
                }
                else
                {
                    e.Substitute = null;
                }


                return;
            };
            string result = generator.Render(this);
            Echo(result);
        }

        private static void escapeInvalidHtml(object sender, TagFormattedEventArgs e)
        {
            if (e.IsExtension)
            {
                // Do not escape text within triple curly braces
                return;
            }
            e.Substitute = SecurityElement.Escape(e.Substitute);
        }

        //http://stackoverflow.com/questions/18634337/how-to-set-filename-containing-spaces-in-content-disposition-header
        public void SendDownload(string fileName, byte[] data, string mimeType = null)
        {
            this.isDownload = true;            

            if (mimeType == null)
            {
                mimeType = "application/octet-stream";
            }

            int size = data.Length;

            if (request.HasVariable("json"))
            {
                fileName = fileName.Replace(' ', '_');
                string content = data.Base64Encode();
                Echo("{\"mimetype\": \"" + mimeType + "\",		\"filename\": \"" + fileName + "\",		\"content\": \"" + content + "\"	}");
            }
            else
            {
                //FIXME
                this.binaryData = data;
            }
        }

        /*public function getPluginList($pluginType, $selectedOption = null)
        {
             $pluginList = array();
             foreach (glob("plugins/$pluginType/*.php") as $file)
             {
                 $extensionName = pathinfo($file, PATHINFO_FILENAME);
                 $pluginList[] = array('name' => $extensionName, 'type' => $pluginType, 'selected' => $selectedOption == $extensionName);
             }
             return $pluginList;
        }

    */


        public string Translate(string key)
        {
            if (this.text.ContainsKey(key))
            {
                return this.text[key];
            }

            return "(?" + key + "?)";
        }

        public Model.File UploadFile(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
            {
                return null;
            }

            var bytes = System.IO.File.ReadAllBytes(filePath);

            var fileName = Path.GetFileName(filePath);
            return UploadFile(fileName, bytes);
        }

        public Model.File UploadFile(string fileName, byte[] bytes)
        {
            var hash = bytes.MD5();

            var condition = Condition.Equal("hash", hash);

            var entity = this.database.FetchEntity<Model.File>(condition);
            if (entity.exists)
            {
                return entity;
            }

            var size = bytes.Length;
            var ext = Path.GetExtension(fileName).Substring(1).ToLower();

            string localName = Utility.GetUniqID() + "." + ext;
            string thumb_fileName = "";

            string uploadFolder = "uploads";

            var targetPath = Path.Combine("public", Path.Combine(uploadFolder, localName));
            targetPath = site.GetFullPath(targetPath);
            System.IO.File.WriteAllBytes(targetPath, bytes);

            switch (ext)
            {
                case "gif":
                case "png":
                case "jpg":
                    {
                        try
                        {
                            thumb_fileName = localName.Replace(ext, "png");
                            thumb_fileName = Path.Combine("thumbs", thumb_fileName);
                            var thumbPath = Path.Combine("public", thumb_fileName);
                            thumbPath = site.GetFullPath(thumbPath);
                            var temp_path = Path.GetDirectoryName(thumbPath);
                            Directory.CreateDirectory(temp_path);

                            /*var img = Image.FromStream(new MemoryStream(bytes));
                            var thumb_img = Utility.ResizeImage(img, 64, 64);
                            thumb_img.Save(thumbPath);*/
                        }
                        catch (Exception e)
                        {
                            thumb_fileName = "";
                        }

                        throw new NotImplementedException();

                        //thumb_bytes = thumb_img.imageToByteArray();
                        break;
                    }

                default:
                    {
                        var iconFile = Path.Combine("extensions", ext + ".png");
                        var tempPath = Path.Combine("public", iconFile);
                        tempPath = site.GetFullPath(tempPath);
                        if (System.IO.File.Exists(tempPath))
                        {
                            thumb_fileName = iconFile;
                        }
                        break;
                    }

            }

            if (!string.IsNullOrEmpty(thumb_fileName))
            {
                thumb_fileName = thumb_fileName.Replace('\\', '/');
                thumb_fileName = "/" + thumb_fileName;
            }

            entity.SetFieldValue("hash", hash);
            entity.SetFieldValue("real_name", fileName);
            entity.SetFieldValue("local_name", "/"+uploadFolder + "/" + localName);
            entity.SetFieldValue("thumb", thumb_fileName);
            entity.SetFieldValue("size", size.ToString());
            entity.Save(this);

            return entity;
        }

        public bool IsValid() //TODO
        {
            var moduleName = this.loadVarFromRequest("module", null);

            return moduleName == null || site.modules.ContainsKey(moduleName);
        }

    }
}
