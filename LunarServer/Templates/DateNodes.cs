using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LunarLabs.WebServer.Templates
{
    public class DateNode : TemplateNode
    {
        private static string[] monthNames = new string[]
        {
            "None", "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"
        };

        private RenderingKey key;
        private string format;

        public DateNode(TemplateDocument document, string key, string format = "dd yyyy | hh:mm tt") : base(document)
        {
            this.key = RenderingKey.Parse(key, RenderingType.DateTime);
            this.format = " " + format;
        }

        public override void Execute(RenderingContext context)
        {
            var temp = context.EvaluateObject(key);

            if (temp != null)
            {
                DateTime value = (DateTime)temp;
                var result = monthNames[value.Month] + value.ToString(format, CultureInfo.InvariantCulture);
                context.output.Append(result);
            }
        }
    }

    public class SpanNode : TemplateNode
    {
        private RenderingKey key;

        public SpanNode(TemplateDocument document, string key) : base(document)
        {
            this.key = RenderingKey.Parse(key, RenderingType.DateTime);
        }

        private static string FetchTranslation(string key, object context)
        {
            /*            try
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
                        }*/

            return key;
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

        public override void Execute(RenderingContext context)
        {
            //var obj = TemplateEngine.EvaluateObject(context, context, "current_time");
            //DateTime cur_time = (obj != null) ? (DateTime)obj : DateTime.Now;

            DateTime cur_time = DateTime.Now;

            var temp = context.EvaluateObject(key);
            if (temp != null)
            {
                DateTime time = (DateTime)temp;

                var diff = cur_time - time;
                var result = FormatTimeSpan(diff, context);
                context.output.Append(result);
            }
        }
    }
}
