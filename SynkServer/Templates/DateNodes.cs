using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SynkServer.Templates
{
    public class DateNode : TemplateNode
    {
        private static string[] monthNames = new string[]
        {
            "None", "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"
        };

        private string key;
        private string format;

        public DateNode(string key, string format = "dd yyyy | hh:mm tt")
        {
            this.key = key;
            this.format = " " + format;
        }

        public override void Execute(Queue<TemplateNode> queue, object context, object pointer, StringBuilder output)
        {
            var temp = TemplateEngine.EvaluateObject(context, pointer, key);

            if (temp != null)
            {
                DateTime value = (DateTime)temp;
                var result = monthNames[value.Month] + value.ToString(format, CultureInfo.InvariantCulture);
                output.Append(result);
            }
        }
    }

    public class SpanNode : TemplateNode
    {
        private string key;

        public SpanNode(string key)
        {
            this.key = key;
        }

        private static string FetchTranslation(string key, object context)
        {
            try
            {
                var translation = (Dictionary<string, string>)(((Dictionary<string, object>)context)["translation"]);
                if (translation != null)
                {
                    var obj = TemplateEngine.EvaluateObject(context, translation, "time_" + key);
                    if (obj != null)
                    {
                        return (string)obj;
                    }
                }

                return key;
            }
            catch
            {
                return key;
            }
        }

        public static string FormatTimeSpan(TimeSpan span, object context)
        {
            string result;

            if (span.TotalMinutes<=5)
            {
                result = FetchTranslation("now", context);
            }
            else
            if (span.TotalHours < 1)
            {
                int minutes = (int)span.TotalMinutes;
                result = minutes + " " + FetchTranslation("minutes", context);
            }
            else
            if (span.TotalDays < 1)
            {
                int hours = (int)span.TotalHours;
                result = hours + " " + FetchTranslation("hours", context);
            }
            else
            if (span.TotalDays < 365)
            {
                int days = (int)span.TotalDays;
                result = span.Days + " " + FetchTranslation("days", context);
            }
            else
            {
                var years = (int)(span.Days / 365);
                result = years + " " + FetchTranslation("years", context);
            }

            return result;
        }

        public override void Execute(Queue<TemplateNode> queue, object context, object pointer, StringBuilder output)
        {
            var obj = TemplateEngine.EvaluateObject(context, context, "current_time");

            DateTime cur_time = (obj != null) ? (DateTime)obj : DateTime.Now;

            var temp = TemplateEngine.EvaluateObject(context, pointer, key);
            if (temp != null)
            {
                DateTime time = (DateTime)temp;

                var diff = cur_time - time;
                var result = FormatTimeSpan(diff, context);
                output.Append(result);
            }
        }
    }
}
