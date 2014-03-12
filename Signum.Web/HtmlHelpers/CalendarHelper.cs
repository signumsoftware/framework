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

namespace Signum.Web
{
    public class DatePickerOptions
    {
        public static DatePickerOptions Default = new DatePickerOptions();
        
        public bool IsDefault()
        {
            return this.ChangeMonth == Default.ChangeMonth &&
                   this.ChangeYear == Default.ChangeYear &&
                   this.FirstDay == Default.FirstDay &&
                   this.YearRange == Default.YearRange &&
                   this.ShowOn == Default.ShowOn &&
                   this.ButtonImageOnly == Default.ButtonImageOnly &&
                   this.ButtonText == Default.ButtonText &&
                   this.ButtonImageSrc == Default.ButtonImageSrc &&
                   this.MinDate == Default.MinDate &&
                   this.MaxDate == Default.MaxDate &&
                   this.ConstrainInput == Default.ConstrainInput;
        }

        public string Format { get; set; }

        public static string JsDateFormat(string dateFormat)
        {
            switch (dateFormat)
            {
                case "d":
                    return CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
                case "D":
                    return CultureInfo.CurrentCulture.DateTimeFormat.LongDatePattern;
                case "f":
                    return CultureInfo.CurrentCulture.DateTimeFormat.LongDatePattern + " " + CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;
                case "F":
                    return CultureInfo.CurrentCulture.DateTimeFormat.FullDateTimePattern;
                case "g":
                    return CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern + " " + CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;
                case "G":
                    return CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern + " " + CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern;
                case "m":
                case "M":
                    return CultureInfo.CurrentCulture.DateTimeFormat.MonthDayPattern;
                case "r":
                case "R":
                    return CultureInfo.CurrentCulture.DateTimeFormat.RFC1123Pattern;
                case "s":
                    return CultureInfo.CurrentCulture.DateTimeFormat.SortableDateTimePattern;
                case "t":
                    return CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;
                case "T":
                    return CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern;
                case "u":
                    return CultureInfo.CurrentCulture.DateTimeFormat.UniversalSortableDateTimePattern;
                case "U":
                    return CultureInfo.CurrentCulture.DateTimeFormat.FullDateTimePattern;
                case "y":
                case "Y":
                    return CultureInfo.CurrentCulture.DateTimeFormat.YearMonthPattern;
            }
            return dateFormat;
        }

        bool changeMonth = true;
        public bool ChangeMonth
        {
            get { return changeMonth; }
            set { changeMonth = value; }
        }

        bool changeYear = true;
        public bool ChangeYear
        {
            get { return changeYear; }
            set { changeYear = value; }
        }

        int firstDay = 1;
        public int FirstDay
        {
            get { return firstDay; }
            set { firstDay = value; }
        }

        string yearRange = "-90:+10";
        public string YearRange
        {
            get { return yearRange; }
            set { yearRange = value; }
        }

        string showOn = "button";
        public string ShowOn
        {
            get { return showOn; }
            set { showOn = value; }
        }

        bool buttonImageOnly = true;
        public bool ButtonImageOnly
        {
            get { return buttonImageOnly; }
            set { buttonImageOnly = value; }
        }

        string buttonText = CalendarMessage.ShowCalendar.NiceToString();
        public string ButtonText
        {
            get { return buttonText; }
            set { buttonText = value; }
        }

        string buttonImageSrc = VirtualPathUtility.ToAbsolute("~/Signum/Images/calendar.png");
        public string ButtonImageSrc
        {
            get { return buttonImageSrc; }
            set { buttonImageSrc = value; }
        }

        string minDate;
        public string MinDate
        {
            get { return minDate; }
            set { minDate = value; }
        }

        string maxDate;
        public string MaxDate
        {
            get { return maxDate; }
            set { maxDate = value; }
        }

        bool constrainInput;
        public bool ConstrainInput
        {
            get { return constrainInput; }
            set { constrainInput = value; }
        }

        public static string DefaultCulture
        {
            get { return CultureInfo.CurrentCulture.Name.Substring(0, 2); }
        }

        public override string ToString()
        {
            return "{" + 
                "changeMonth:{0}, changeYear:{1}, firstDay:{2}, yearRange:'{3}', showOn:'{4}', buttonImageOnly:{5}, buttonText:'{6}', buttonImage:'{7}', constrainInput: {8}{9}{10}{11}".Formato(
                    ChangeMonth ? "true" : "false",
                    ChangeYear ? "true" : "false",
                    FirstDay,
                    YearRange,
                    ShowOn,
                    ButtonImageOnly ? "true" : "false",
                    ButtonText,
                    ButtonImageSrc,
                    ConstrainInput ? "true" : "false",
                    (MinDate.HasText() ? ", minDate: " + MinDate : ""),
                    (MaxDate.HasText() ? ", maxDate: " + MaxDate : ""),
                    (Format.HasText() ? ", dateFormat: '" + JsDateFormat(Format) + "'" : "") + 
                "}");
        }
    }

    public static class CalendarHelper
    {
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
