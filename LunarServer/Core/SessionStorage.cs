using LunarLabs.Parser;
using LunarLabs.Parser.JSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LunarLabs.WebServer.Core
{
    public abstract class SessionStorage
    {
        public TimeSpan CookieExpiration { get; set; } = TimeSpan.FromMinutes(30);
        public TimeSpan UpdateFrequency { get; set; } = TimeSpan.FromSeconds(30);

        private DateTime lastUpdateTime = DateTime.MinValue;

        public abstract Session CreateSession(string ID = null);
        public abstract bool HasSession(string ID);

        public abstract Session GetSession(string ID);

        public abstract void Save();
        public abstract void Restore();

        protected abstract void ExpireOldSessions();

        public void Update()
        {
            var currentTime = DateTime.UtcNow;
            var diff = currentTime - lastUpdateTime;
            if (diff.TotalSeconds > 30)
            {
                ExpireOldSessions();
                Save();
                lastUpdateTime = currentTime;
            }
        }
    }

    public class MemorySessionStorage : SessionStorage
    {
        protected Dictionary<string, Session> _sessions = new Dictionary<string, Session>(StringComparer.InvariantCultureIgnoreCase);

        protected override void ExpireOldSessions()
        {
            lock (_sessions)
            {
                var allKeys = _sessions.Keys.ToArray();
                foreach (var key in allKeys)
                {
                    var sessionInfo = _sessions[key];
                    if (DateTime.Now.Subtract(sessionInfo.lastActivity) > this.CookieExpiration)
                    {
                        _sessions.Remove(key);
                    }
                }
            }
        }

        public override Session CreateSession(string ID = null)
        {
            var session = new Session(ID);
            lock (_sessions)
            {
                _sessions[session.ID] = session;
            }
            return session;
        }

        public override bool HasSession(string ID)
        {
            return _sessions.ContainsKey(ID);
        }

        public override Session GetSession(string ID)
        {
            lock (_sessions)
            {
                if (_sessions.ContainsKey(ID))
                {
                    return _sessions[ID];
                }
            }

            return null;
        }

        public override void Save()
        {
            // do nothing
        }

        public override void Restore()
        {
            lock (_sessions)
            {
                _sessions.Clear();
            }
        }
    }

    public class FileSessionStorage : MemorySessionStorage
    {
        private string fileName;

        public FileSessionStorage(string fileName)
        {
            this.fileName = fileName;
        }

        public override void Save()
        {
            if (_sessions.Count > 0)
            {
                var root = DataNode.CreateArray("sessions");

                foreach (var session in _sessions.Values)
                {
                    var node = DataNode.CreateObject();
                    node.AddField("ID", session.ID);
                    node.AddField("activity", session.lastActivity);
                    root.AddNode(node);

                    var contents = DataNode.CreateObject("contents");
                    node.AddNode(contents);

                    foreach (var entry in session.Data)
                    {
                        var type = entry.Value.GetType();

                        string typeName;

                        if (type == typeof(string))
                        {
                            typeName = "string";
                        }
                        else
                        if (type == typeof(bool))
                        {
                            typeName = "bool";
                        }
                        else
                        if (type == typeof(int))
                        {
                            typeName = "int";
                        }
                        else
                        {
                            typeName = null;
                        }

                        if (typeName == null)
                        {
                            continue;
                        }

                        contents.AddField(entry.Key, entry.Value);
                    }
                }

                var json = JSONWriter.WriteToString(root);
                File.AppendAllText(fileName, json);
            }
        }

        public override void Restore()
        {
            base.Restore();
            
            if (File.Exists(fileName))
            {
                var json = File.ReadAllText(fileName);
                var root = JSONReader.ReadFromString(json);

                root = root["sessions"];

                foreach (var child in root.Children)
                {                   
                    var ID = child.GetString("ID");
                    var session = this.CreateSession(ID);
                    session.lastActivity = child.GetDateTime("activity");

                    var contents = child.GetNode("contents");
                    if (contents != null)
                    {
                        foreach (var content in contents)
                        {
                            var key = content.Name;

                            switch (content.Kind)
                            {
                                case NodeKind.String: session.SetString(key, content.Value); break;

                                case NodeKind.Boolean:
                                    {
                                        var val = bool.Parse(content.Value);
                                        session.SetBool(key, val);
                                        break;
                                    }

                                case NodeKind.Numeric:
                                    {
                                        var val = int.Parse(content.Value);
                                        session.SetInt(key, val);
                                        break;
                                    }

                                default: throw new Exception("Unknown session object type: " + content.Kind);
                            }
                        }
                    }

                }
            }
        }
    }
}
