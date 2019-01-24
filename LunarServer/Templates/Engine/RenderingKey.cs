using System;
using System.Collections.Generic;

namespace LunarLabs.Templates
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

    public enum KeyOperator
    {
        Equal,
        Different,
        Greater,
        Less,
        GreaterOrEqual,
        LessOrEqual,
        Assignment,
        Contains,
        Plus,
        Multiply,
    }

    public abstract class RenderingKey
    {
        public abstract RenderingType RenderingType { get;}

        private static readonly Dictionary<string, KeyOperator> operatorSymbols = new Dictionary<string, KeyOperator>()
        {
            { "==" , KeyOperator.Equal},
            { "!=" , KeyOperator.Different},
            { ">" , KeyOperator.Greater},
            { "<", KeyOperator.Less },
            { ">=" , KeyOperator.GreaterOrEqual},
            { "<=" , KeyOperator.LessOrEqual},
            { ":=" , KeyOperator.Assignment},
            { "+" , KeyOperator.Plus},
            { "*" , KeyOperator.Multiply},
            { "contains" , KeyOperator.Contains},
        };

        public static RenderingKey Parse(string key, RenderingType expectedType)
        {
            if (key.StartsWith("!"))
            {
                if (expectedType != RenderingType.Bool && expectedType != RenderingType.Any)
                {
                    throw new Exception("expected bool");
                }

                key = key.Substring(1);
                var body = Parse(key, expectedType);
                return new NegationKey(body);
            }

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

            if (key.StartsWith("@"))
            {
                return new GlobalKey(key.Substring(1));
            }

            switch (key)
            {
                case "true":
                    if (expectedType != RenderingType.Bool && expectedType != RenderingType.Any)
                    {
                        throw new Exception("expected bool");
                    }

                    return new LiteralKey(true, RenderingType.Bool);

                case "false":
                    if (expectedType != RenderingType.Bool && expectedType != RenderingType.Any)
                    {
                        throw new Exception("expected bool");
                    }

                    return new LiteralKey(false, RenderingType.Bool);

                case "this":
                    return new SelfKey();
            }

            if (key.StartsWith("'") && key.EndsWith("'"))
            {
                if (expectedType != RenderingType.String && expectedType != RenderingType.Any)
                {
                    throw new Exception("expected string");
                }

                var str = key.Substring(1, key.Length - 2);
                return new LiteralKey(str, RenderingType.String);
            }

            decimal number;
            if (decimal.TryParse(key, out number))
            {
                if (expectedType != RenderingType.Numeric && expectedType != RenderingType.Any)
                {
                    throw new Exception("expected number");
                }

                return new LiteralKey(number, RenderingType.Numeric);
            }

            return new PathRenderingKey(key);
        }

        public abstract object Evaluate(RenderingContext context);
    }
}
