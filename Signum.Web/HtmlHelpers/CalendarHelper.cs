using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Utilities;
using System.Globalization;
using System.Web.Script.Serialization;
using System.Web;
using System.Linq.Expressions;
using Signum.Web.Lines;
using System.Web.Mvc.Html;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using System.Text.RegularExpressions;
using Signum.Utilities.DataStructures;

namespace Signum.Web
{
    public static class CalendarHelper
    {
        static readonly Regex parts = new Regex(@"(\w)\1{0,}");

        public static string GetDatePickerFormat(string dateTimeFormat, CultureInfo culture = null)
        {
            if (culture == null)
                culture = CultureInfo.CurrentCulture;

            string format = Customize(dateTimeFormat, culture.DateTimeFormat);

            var js = parts.Replace(format, m => ToJs(m.Value));

            return Clean(js);
        }

        private static string Clean(string js)
        {
            var matches = parts.Matches(js).Cast<Match>();

            if(matches.All(m=>m.Value == "X"))
                return js;

            matches.Where(m=>m.Value != "X")
                .Select(m=>new Interval<int>(m.Index, m.Index + m.Length))
                .Aggregate((a,b)=>a.Union(b));



            matches.Where(m => m.Value != "X").Min(a=>a.
        }

        private static string ToJs(string part)
        {
            switch (part)
            {
                case "d": return "d";
                case "dd": return "dd";
                case "ddd": return "D";
                case "dddd": return "DD";
                case "M": return "m";
                case "MM": return "mm";
                case "MMM": return "M";
                case "MMMM": return "MM";
                case "yy": return "yy";
                case "yyyy": return "yyyy";
                default: return "X";
            }
        }

        private static string Customize(string format, DateTimeFormatInfo info)
        {
            switch (format)
            {
                case "d": return info.ShortDatePattern;
                case "D": return info.LongDatePattern;
                case "f": return info.LongDatePattern + " " + info.ShortTimePattern;
                case "F": return info.FullDateTimePattern;
                case "g": return info.ShortDatePattern + " " + info.ShortTimePattern;
                case "G": return info.ShortDatePattern + " " + info.LongTimePattern;
                case "m":
                case "M": return info.MonthDayPattern;
                case "r":
                case "R": return info.RFC1123Pattern;
                case "s": return info.SortableDateTimePattern;
                case "t": return info.ShortTimePattern;
                case "T": return info.LongTimePattern;
                case "u": return info.UniversalSortableDateTimePattern;
                case "U": return info.FullDateTimePattern;
                case "y":
                case "Y": return info.YearMonthPattern;
                default: return format;
            }
        }

        public static MvcHtmlString Calendar(this HtmlHelper helper, string elementId, DatePickerOptions settings)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(
                "<script type=\"text/javascript\">" + 
                //"$(function(){\n" +
                "$(\"#" + elementId + "\").datepicker(" + settings.ToString() +");" + 
                //"});\n" +
                "</script>");

            return MvcHtmlString.Create(sb.ToString());
        }

        public static MvcHtmlString HourMinute<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property)
        {
            return helper.HourMinute(tc, property, null);
        }

        public static MvcHtmlString HourMinute<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<HourMinuteLine> settingsModifier)
        {
            TypeContext<S> context = Common.WalkExpression(tc, property);

            HourMinuteLine line = new HourMinuteLine(typeof(S), context.Value, context, null, context.PropertyRoute);

            Common.FireCommonTasks(line);

            if (settingsModifier != null)
                settingsModifier(line);

            return HourMinuteInternal(helper, line);
        }

        private static MvcHtmlString HourMinuteInternal(HtmlHelper helper, HourMinuteLine line)
        {
            if (!line.Visible || (line.HideIfNull && line.UntypedValue == null))
                return MvcHtmlString.Empty;

            HtmlStringBuilder sb = new HtmlStringBuilder();

            TimeSpan? time = null;
            
            DateTime? dateValue = line.UntypedValue as DateTime?;
            if (dateValue != null)
                time = dateValue.TrySS(d => d.ToUserInterface()).TrySS(d => d.TimeOfDay);
            else
                time = line.UntypedValue as TimeSpan?;

            WriteField(sb, helper, line, QueryTokenMessage.Hour.NiceToString(), "Hour", time == null ? "" : time.Value.ToString("hh"));

            sb.Add(helper.Span("", ":", "form-control sf-time-separator", new Dictionary<string, object> { { "style", "font-weight:bold" } }));

            WriteField(sb, helper, line, QueryTokenMessage.Minute.NiceToString(), "Minute", time == null ? "" : time.Value.ToString("mm")); 

            return sb.ToHtml();
        }

        private static void WriteField(HtmlStringBuilder sb, HtmlHelper helper, HourMinuteLine line, string label, string name, string value)
        {
            throw new InvalidOperationException();
            //using (sb.Surround(new HtmlTag("div").Class("sf-field")))
            //{
            //    if (line.LabelVisible)
            //        sb.Add(new HtmlTag("div").Class("sf-label-line").SetInnerText(label));

            //    using (sb.Surround(new HtmlTag("div").Class("sf-value-container")))
            //    {
            //        if (line.ReadOnly)
            //        {
            //            sb.Add(new HtmlTag("span").Class("sf-value-line").SetInnerText(value));

            //            if (line.WriteHiddenOnReadonly)
            //                sb.Add(helper.Hidden(line.Compose(name), value));
            //        }
            //        else
            //        {
            //            line.ValueHtmlProps["onblur"] = "this.setAttribute('value', this.value); " + line.ValueHtmlProps.TryGetC("onblur");

            //            line.ValueHtmlProps["size"] = "2";
            //            line.ValueHtmlProps["class"] = "sf-value-line";

            //            sb.Add(helper.TextBox(line.Compose(name), value, line.ValueHtmlProps));
            //        }
            //    }
            //}
        }
    }
}
