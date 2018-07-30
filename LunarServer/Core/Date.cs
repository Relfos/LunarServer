using System;
using System.Collections.Generic;
using System.Text;

namespace LunarLabs.WebServer.Core
{
    public struct Date
    {
        public int year;
        public int month;
        public int day;

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                hash = hash * 23 + year.GetHashCode();
                hash = hash * 23 + month.GetHashCode();
                hash = hash * 23 + day.GetHashCode();
                return hash;
            }
        }
    }

    public static class DateUtils
    {
        public static Date FromDateTime(this DateTime date)
        {
            return new Date() { year = date.Year, month = date.Month, day = date.Day };
        }

        public static DateTime FromDate(this Date date)
        {
            return new DateTime(date.year, date.month, date.day);
        }

        public static DateTime FixedDay(this DateTime val)
        {
            return new DateTime(val.Year, val.Month, val.Day);
        }

        public static DateTime FixedMonth(this DateTime val)
        {
            return new DateTime(val.Year, val.Month, 1);
        }

        public static DateTime FixedYear(this DateTime val)
        {
            return new DateTime(val.Year, 1, 1);
        }

    }
}
