using System;
using System.Collections.Generic;
using System.Linq;

namespace LunarLabs.WebServer.Core
{
    public class Session
    {
        private Dictionary<string, object> _data = new Dictionary<string, object>();

        public IEnumerable<KeyValuePair<string, object>> Data { get { return _data; } }

        public string ID;
        public DateTime lastActivity;

        public Session(string ID = null)
        {
            this.ID = ID != null ? ID : Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + DateTime.Now.Millisecond.ToString() + DateTime.Now.Ticks.ToString()));
            this.lastActivity = DateTime.Now;
        }

        public bool Contains(string name)
        {
            return _data.ContainsKey(name);
        }

        private T Get<T>(string name, T defaultValue = default(T))
        {
            if (_data.ContainsKey(name))
            {
                return (T)_data[name];
            }

            return defaultValue;
        }

        private void Set(string name, object value)
        {
            _data[name] = value;
        }

        public void SetString(string name, string value)
        {
            Set(name, value);
        }

        public void SetBool(string name, bool value)
        {
            Set(name, value);
        }

        public void SetInt(string name, int value)
        {
            Set(name, value);
        }

        public void SetStruct<T>(string name, object obj) where T : struct 
        {
            var type = typeof(T);
            var fields = type.GetFields();
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(string))
                {
                    var val = (string)field.GetValue(obj);
                    SetString(name + "." + field.Name, val);
                }
                else
                if (field.FieldType == typeof(bool))
                {
                    var val = (bool)field.GetValue(obj);
                    SetBool(name + "." + field.Name, val);
                }
                else
                if (field.FieldType == typeof(int))
                {
                    var val = (int)field.GetValue(obj);
                    SetInt(name + "." + field.Name, val);
                }
                else
                {
                    throw new Exception("Unsupport field type for session storage: " + field.FieldType.Name);
                }
            }
        }

        public string GetString(string name, string defaultVal = null)
        {
            return Get<string>(name, defaultVal);
        }

        public bool GetBool(string name, bool defaultVal = false)
        {
            return Get<bool>(name, defaultVal);
        }

        public int GetInt(string name, int defaultVal = 0)
        {
            return Get<int>(name, defaultVal);
        }

        public T GetStruct<T>(string name) where T : struct
        {
            var type = typeof(T);
            var fields = type.GetFields();
            T obj = default(T);
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(string))
                {
                    var val = GetString(name + "." + field.Name);
                    field.SetValue(obj, val);
                }
                else
                if (field.FieldType == typeof(bool))
                {
                    var val = GetBool(name + "." + field.Name);
                    field.SetValue(obj, val);
                }
                else
                if (field.FieldType == typeof(int))
                {
                    var val = GetInt(name + "." + field.Name);
                    field.SetValue(obj, val);
                }
                else
                {
                    throw new Exception("Unsupport field type for session storage: " + field.FieldType.Name);
                }
            }
            return obj;
        }

        public void Remove(string name)
        {
            if (_data.ContainsKey(name))
            {
                _data.Remove(name);
            }            
        }

        public void Destroy()
        {
            _data.Clear();
        }

    }

}
