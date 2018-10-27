using System;
using System.Collections.Generic;

namespace LunarLabs.WebServer.Templates
{
    public class CompositeRenderingKey : RenderingKey
    {
        private RenderingKey leftSide;
        private RenderingKey rightSide;

        private KeyOperator _operator;

        internal CompositeRenderingKey(KeyOperator op, string leftText, string rightText)
        {
            _operator = op;

            RenderingType expectedType;

            switch (_operator)
            {
                case KeyOperator.Equal:
                case KeyOperator.Different:
                    expectedType = RenderingType.Any;
                    break;

                default:
                    expectedType = RenderingType.Numeric;
                    break;
                    
            }

            this.leftSide = RenderingKey.Parse(leftText, expectedType);
            this.rightSide = RenderingKey.Parse(rightText, expectedType);
        }

        private static decimal EvaluateNumber(object obj)
        {
            if (obj == null)
            {
                return 0;
            }

            var text = obj.ToString();
            decimal result;

            if (decimal.TryParse(text, out result))
            {
                return result;
            }

            return 0;
        }

        private static bool InternalEvaluate(KeyOperator op, object left, object right)
        {
            switch (op)
            {
                case KeyOperator.Different:
                    return !InternalEvaluate(KeyOperator.Equal, left, right);

                case KeyOperator.LessOrEqual:
                    return !InternalEvaluate(KeyOperator.Greater, left, right);

                case KeyOperator.GreaterOrEqual:
                    return !InternalEvaluate(KeyOperator.Less, left, right);

                case KeyOperator.Equal:
                    {
                        if (left == null && right == null)
                        {
                            return true;
                        }
                        else
                        if ((left == null && right != null) || (left != null && right == null))
                        {
                            return false;
                        }
                        else
                        {
                            return left.ToString().Equals(right.ToString());
                        }
                    }

                case KeyOperator.Less:
                    {
                        var leftNumber = EvaluateNumber(left);
                        var rightNumber = EvaluateNumber(right);
                        return leftNumber < rightNumber;
                    }

                case KeyOperator.Greater:
                    {
                        var leftNumber = EvaluateNumber(left);
                        var rightNumber = EvaluateNumber(right);
                        return leftNumber > rightNumber;
                    }

                default:
                    throw new NotImplementedException();
            }
        }

        public override object Evaluate(RenderingContext context)
        {
            var left = this.leftSide.Evaluate(context);
            var right = this.rightSide.Evaluate(context);
            return InternalEvaluate(_operator, left, right);
        }
    }
}
