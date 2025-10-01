using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LunarLabs.WebMVC
{
    public class Condition
    {
        public enum Operator
        {
            And,
            Or,
            Contains,
            BeginsWith,
            EndsWith,
            Equals,
            LessThan,
            GreaterThan,
            LessOrEqualThan,
            GreaterOrEqualThan,
            NotEqual
        }

        public Operator op;
        public string fieldName;
        public string opValue;

        public Condition childA;
        public Condition childB;

        public Condition(Operator op, string fieldName, string val)
        {
            this.fieldName = fieldName;
            this.op = op;
            this.opValue = val;
            this.childA = null;
            this.childB = null;
        }

        public Condition(Operator op, Condition A, Condition B)
        {
            this.fieldName = null;
            this.op = op;
            this.opValue = null;
            this.childA = A;
            this.childB = B;
        }

        public static Condition And(Condition A, Condition B)
        {
            return new Condition(Operator.And, A, B);
        }

        public static Condition Or(Condition A, Condition B)
        {
            return new Condition(Operator.Or, A, B);
        }

        public static Condition Equal(string fieldName, string val)
        {
            return new Condition(Operator.Equals, fieldName,  val);
        }

        public static Condition Contains(string fieldName, string val)
        {
            return new Condition(Operator.Contains, fieldName, val);
        }

    }
}
