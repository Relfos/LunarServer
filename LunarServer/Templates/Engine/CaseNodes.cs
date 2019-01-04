using System.Linq;

//http://wiki.c2.com/?CapitalizationRules

namespace LunarLabs.Templates
{
    public enum CaseKind
    {
        Lower,
        Upper,
        Pascal,
        Snake,
        Camel,
        Kebab
    }

    public class CaseNode : TemplateNode
    {
        private CaseKind kind;
        private RenderingKey key;

        public CaseNode(Document document, string key, CaseKind kind) : base(document)
        {
            this.key = RenderingKey.Parse(key, RenderingType.String);
            this.kind = kind;
        }

        private static string TitleCase(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            char[] a = s.ToCharArray();
            a[0] = char.ToUpperInvariant(a[0]);

            return new string(a);
        }

        private static string CamelCase(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            char[] a = s.ToCharArray();
            a[0] = char.ToLowerInvariant(a[0]);

            return new string(a);
        }

        public override void Execute(RenderingContext context)
        {
            var temp = context.EvaluateObject(key);

            if (temp != null)
            {
                var value = temp.ToString();
                string result;
                switch (kind)
                {
                    case CaseKind.Lower: result = value.ToLowerInvariant(); break;
                    case CaseKind.Upper: result = value.ToUpperInvariant(); break;
                    case CaseKind.Pascal: result = TitleCase(value); break;
                    case CaseKind.Camel: result = CamelCase(value); break;
                    case CaseKind.Snake: result = string.Concat(value.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())); break;
                    case CaseKind.Kebab: result = value.Replace(' ', '-').ToLowerInvariant(); break;
                    default: result = value; break;
                }
                context.output.Append(result);
            }
        }
    }
}
