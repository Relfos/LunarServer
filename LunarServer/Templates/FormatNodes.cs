using LunarLabs.Server.Utils;
using System.Collections.Generic;
using System.Text;

namespace LunarLabs.WebServer.Templates
{
    public class NumericFormatNode : TemplateNode
    {
        internal static readonly Dictionary<string, decimal> AmountFormatters = new Dictionary<string, decimal>()
        {
            { "B", 1000000000},
            { "M" , 1000000},
            { "K", 1000},
        };

        internal static readonly Dictionary<string, decimal> SizeFormatters = new Dictionary<string, decimal>()
        {
            { "GB", 1000000000},
            { "MB", 1000000},
            { "KB", 1000},
        };

        private string key;
        private Dictionary<string, decimal> dictionary;

        public NumericFormatNode(TemplateDocument document, string key, Dictionary<string, decimal> dictionary) : base(document)
        {
            this.key = key;
            this.dictionary = dictionary;
        }

        public override void Execute(RenderingContext context)
        {
            var temp = context.EvaluateObject(key);

            if (temp != null)
            {
                decimal num = (decimal)temp;

                string key = null;
                foreach (var entry in dictionary)
                {
                    if (num >= entry.Value || num<= -entry.Value)
                    {
                        key = entry.Key;
                    }
                }

                if (key != null)
                {
                    num /= dictionary[key];
                    num = num.TruncateEx(2);
                    context.output.Append(num.ToString());
                    context.output.Append(key);
                }
                else
                {
                    context.output.Append(num.ToString());
                }
            }
        }
    }

}
