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

        public static string FormatTimeSpan(TimeSpan span)
        {
            string result;

            if (span.TotalHours < 1)
            {
                result = span.Minutes + " minutes";
            }
            else
            if (span.TotalDays < 1)
            {
                result = span.Hours + " hours";
            }
            else
            {
                result = span.Days + " days";
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
                var result = FormatTimeSpan(diff);
                output.Append(result);
            }
        }
    }
}
