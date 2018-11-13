using LunarLabs.Parser;
using LunarLabs.WebServer.Core;
using System;
using System.Collections;
using System.Collections.Generic;

namespace LunarLabs.WebServer.Templates
{
    public enum RenderingType
    {
        Any,
        String,
        Bool,
        Numeric,
        Collection,
        DateTime,
    }

    internal enum KeyOperator
    {
        Equal,
        Different,
        Greater,
        Less,
        GreaterOrEqual,
        LessOrEqual,
    }

    public abstract class RenderingKey
    {
        private static readonly Dictionary<string, KeyOperator> operatorSymbols = new Dictionary<string, KeyOperator>()
        {
            { "==" , KeyOperator.Equal},
            { "!=" , KeyOperator.Different},
            { ">" , KeyOperator.Greater},
            { "<", KeyOperator.Less },
            { ">=" , KeyOperator.GreaterOrEqual},
            { "<=" , KeyOperator.LessOrEqual},
        };

        public static RenderingKey Parse(string key, RenderingType expectedType)
        {
            string operatorMatch = null;
            int operatorIndex = -1;

            foreach (var symbol in operatorSymbols.Keys)
            {
                if (operatorMatch != null && symbol.Length < operatorMatch.Length)
                {
                    continue;
                }

                var index = key.IndexOf(symbol);

                if (index >= 0)
                {
                    operatorIndex = index;
                    operatorMatch = symbol;
                }
            }

            if (operatorMatch != null)
            {
                var leftText = key.Substring(0, operatorIndex).Trim();
                var righText = key.Substring(operatorIndex + operatorMatch.Length).Trim();

                var op = operatorSymbols[operatorMatch];
                return new CompositeRenderingKey(op, leftText, righText);
            }
            else
            {
                return new SingleRenderingKey(key, expectedType);
            }
        }

        public abstract object Evaluate(RenderingContext context);
    }
}
