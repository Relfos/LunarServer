﻿using System.Collections.Generic;
using System.Text;

namespace LunarLabs.Templates
{
    public enum RenderingOperation
    {
        None,
        Continue,
        Break,
    }

    public class RenderingContext
    {
        public Queue<Document> queue;
        public object DataRoot;
        public List<object> DataStack;
        public StringBuilder output;

        internal RenderingOperation operation;

        private Dictionary<string, object> variables;

        public void Set(string key, object val)
        {
            if (variables == null)
            {
                variables = new Dictionary<string, object>();
            }

            variables[key] = val;
        }

        public object Get(string key)
        {
            if (variables == null)
            {
                return null;
            }

            if (variables.ContainsKey(key))
            {
                return variables[key];
            }

            return null;
        }

        public object EvaluateObject(RenderingKey key)
        {
            if (key == null)
            {
                return null;
            }

            return key.Evaluate(this);
        }
    }

}
