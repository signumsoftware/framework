using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Web.Properties;
using System.Globalization;
using System.Web.Script.Serialization;
using System.Web;

namespace Signum.Web
{
    public class DatePickerOptions
    {
        public static DatePickerOptions Default = new DatePickerOptions();
        
        public bool IsDefault()
        {
            return this.ShowAge == Default.ShowAge &&
                   this.ChangeMonth == Default.ChangeMonth &&
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

        bool showAge = false;
        /// <summary>
        /// If true it will show age next no datetimepicker and will refresh if the value of datepicker changes
        /// </summary>
        public bool ShowAge
        {
            get { return showAge; }
            set { showAge = value; }
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

        string buttonText = Resources.ShowCalendar;
        public string ButtonText
        {
            get { return buttonText; }
            set { buttonText = value; }
        }

        string buttonImageSrc = VirtualPathUtility.ToAbsolute("~/signum/Images/calendar.png");
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

        [ThreadStatic]
        static string defaultculture;
        public static string DefaultCulture
        {
            get { return defaultculture ?? CultureInfo.CurrentCulture.Name.Substring(0, 2); }
            set { defaultculture = value; }
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
                "<script type=\"text/javascript\">\n" + 
                "$(function(){\n" +
                "$(\"#" + elementId + "\").datepicker(" + settings.ToString() +");\n" + 
                "});\n" +
                "</script>");

            return MvcHtmlString.Create(sb.ToString());
        }     
    }
}
