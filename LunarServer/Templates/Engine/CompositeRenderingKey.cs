using System;
using System.Collections;

namespace LunarLabs.Templates
{
    public class CompositeRenderingKey : RenderingKey
    {
        public RenderingKey leftSide { get; private set; }
        public RenderingKey rightSide { get; private set; }

        public KeyOperator Operator { get; private set; }

        public override RenderingType RenderingType
        {
            get
            {
                if (leftSide.RenderingType == rightSide.RenderingType)
                {
                    return leftSide.RenderingType;
                }

                return RenderingType.Any;
            }
        }

        internal CompositeRenderingKey(KeyOperator op, string leftText, string rightText)
        {
            Operator = op;

            RenderingType expectedType;

            switch (Operator)
            {
                case KeyOperator.Equal:
                case KeyOperator.Different:
                case KeyOperator.Assignment:
                case KeyOperator.Contains:
                case KeyOperator.And:
                case KeyOperator.Or:
                    expectedType = RenderingType.Any;
                    break;

                case KeyOperator.Begins:
                case KeyOperator.Ends:
                    expectedType = RenderingType.String;
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

        private static bool EvaluateBool(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var text = obj.ToString();
            return text.Equals("true", StringComparison.InvariantCultureIgnoreCase);
        }

        private static object InternalEvaluate(KeyOperator op, object left, object right)
        {
            switch (op)
            {
                case KeyOperator.Different:
                    return !(bool)InternalEvaluate(KeyOperator.Equal, left, right);

                case KeyOperator.LessOrEqual:
                    return !(bool)InternalEvaluate(KeyOperator.Greater, left, right);

                case KeyOperator.GreaterOrEqual:
                    return !(bool)InternalEvaluate(KeyOperator.Less, left, right);

                case KeyOperator.And:
                    {
                        var leftVal = EvaluateBool(left);
                        var rightVal = EvaluateBool(right);
                        return leftVal && rightVal;
                    }

                case KeyOperator.Or:
                    {
                        var leftVal = EvaluateBool(left);
                        var rightVal = EvaluateBool(right);
                        return leftVal || rightVal;
                    }

                case KeyOperator.Contains:
                    {
                        if (right == null)
                        {
                            return false;
                        }

                        var rightVal = right.ToString();
                        if (string.IsNullOrEmpty(rightVal))
                        {
                            return false;
                        }

                        if (left == null)
                        {
                            return false;
                        }

                        if (left is IEnumerable)
                        {
                            var list = (IEnumerable)left;
                            foreach (var entry in list)
                            {
                                if (entry.ToString() == rightVal)
                                {
                                    return true;
                                }
                            }
                            return false;
                        }
                        else
                        {
                            var leftVal = left.ToString();
                            return leftVal.Contains(rightVal);
                        }
                    }

                case KeyOperator.Begins:
                    {
                        var leftVal = left.ToString();
                        var rightVal = right.ToString();
                        return leftVal.StartsWith(rightVal);
                    }

                case KeyOperator.Ends:
                    {
                        var leftVal = left.ToString();
                        var rightVal = right.ToString();
                        return leftVal.EndsWith(rightVal);
                    }

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

                case KeyOperator.Plus:
                    {
                        var leftNumber = EvaluateNumber(left);
                        var rightNumber = EvaluateNumber(right);
                        return leftNumber + rightNumber;
                    }

                case KeyOperator.Multiply:
                    {
                        var leftNumber = EvaluateNumber(left);
                        var rightNumber = EvaluateNumber(right);
                        return leftNumber * rightNumber;
                    }

                default:
                    throw new NotImplementedException();
            }
        }

        public override object Evaluate(RenderingContext context)
        {
            var left = this.leftSide.Evaluate(context);
            var right = this.rightSide.Evaluate(context);
            var result = InternalEvaluate(Operator, left, right);
            return result;
        }
    }
}
