﻿using System;
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

        public bool IsEmpty => _data.Count == 0;
        public int Size => _data.Count;

        public Session(string ID = null)
        {
            this.ID = ID != null ? ID : Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + DateTime.Now.Millisecond.ToString() + DateTime.Now.Ticks.ToString()));
            this.lastActivity = DateTime.Now;
        }

        public bool Contains(string name)
        {
            if (name.EndsWith("*"))
            {
                name = name.Substring(0, name.Length - 1);
                foreach (var entry in _data)
                {
                    if (entry.Key.StartsWith(name))
                    {
                        return true;
                    }
                }
                return false;
            }

            if (name.StartsWith("*"))
            {
                name = name.Substring(1);
                foreach (var entry in _data)
                {
                    if (entry.Key.EndsWith(name))
                    {
                        return true;
                    }
                }
                return false;
            }

            return _data.ContainsKey(name);
        }

        private T Get<T>(string name, T defaultValue = default(T))
        {
            if (_data.ContainsKey(name))
            {
                try
                {
                    var temp = (T)_data[name];
                    return temp;
                }
                catch (Exception e)
                {
                    return defaultValue;
                }
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
            var ptr = __makeref(obj);
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(string))
                {
                    var val = GetString(name + "." + field.Name);
                    field.SetValueDirect(ptr, val);
                }
                else
                if (field.FieldType == typeof(bool))
                {
                    var val = GetBool(name + "." + field.Name);
                    field.SetValueDirect(ptr, val);
                }
                else
                if (field.FieldType == typeof(int))
                {
                    var val = GetInt(name + "." + field.Name);
                    field.SetValueDirect(ptr, val);
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
