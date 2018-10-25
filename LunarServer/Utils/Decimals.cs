using System;
using System.Collections.Generic;
using System.Text;

namespace LunarLabs.Server.Utils
{
    public static class DecimalExtensions
    {
        public static decimal DecimalPlaces(this decimal value)
        {
            var precision = (Decimal.GetBits(value)[3] >> 16) & 0x000000FF;
            return precision;
        }

        public static decimal TruncateEx(this decimal value, int decimalPlaces)
        {
            if (decimalPlaces < 0)
                throw new ArgumentException("decimalPlaces must be greater than or equal to 0.");

            var precision = DecimalPlaces(value);
            if (precision <= decimalPlaces)
                return value;

            var modifier = Convert.ToDecimal(0.5 / Math.Pow(10, decimalPlaces));
            return Math.Round(value >= 0 ? value - modifier : value + modifier, decimalPlaces);
        }

        public static Decimal Lerp(decimal A, decimal B, decimal t)
        {
            return A * (1m - t) + B * t;
        }

        public static Decimal GetPercentChange(decimal prevValue, decimal curValue)
        {
            if (prevValue == 0)
            {
                return 0;
            }

            return 100 * (curValue > prevValue ? (curValue / prevValue) - 1 : (1m - (curValue / prevValue)) * -1);
        }
    }
}
