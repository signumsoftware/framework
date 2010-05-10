using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Web.Properties;
using System.Globalization;

namespace Signum.Web
{
    public class DatePickerOptions
    {
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

        string buttonImageSrc = "Scripts/jqueryui/images/calendar.png";
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
            get { return defaultculture ?? CultureInfo.CurrentCulture.Name.Substring(0,2); }
            set { defaultculture = value; }
        }
    }

    public static class CalendarHelper
    {
        public static Action<HtmlHelper, StringBuilder> IncludeCss;
        public static string jQueryPrefix = "";
        //jQuery ui DatePicker
        public static string Calendar(this HtmlHelper helper, string elementId, DatePickerOptions settings)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(helper.DynamicScriptInclude(
                "Scripts/jqueryui/" + jQueryPrefix + "ui.core.js",
                "Scripts/jqueryui/" + jQueryPrefix + "ui.datepicker.js",
                "Scripts/jqueryui/i18n/" + jQueryPrefix + "ui.datepicker-" + DatePickerOptions.DefaultCulture + ".js"));

            if (IncludeCss != null)
                IncludeCss(helper, sb);
            else
                sb.AppendLine(helper.DynamicCssInclude("Scripts/jqueryui/" + jQueryPrefix + "ui.all.css",
                    "Scripts/jqueryui/" + jQueryPrefix + "ui.base.css",
                    "Scripts/jqueryui/" + jQueryPrefix + "ui.core.css",
                    "Scripts/jqueryui/" + jQueryPrefix + "ui.datepicker.css",
                    "Scripts/jqueryui/" + jQueryPrefix + "ui.theme.css"));

            sb.AppendLine(
                "<script type=\"text/javascript\">\n" + 
                "$(document).ready(function(){\n" +
                "$(\"#" + elementId + "\").datepicker({ " + OptionsToString(settings) +" });\n" + 
                "});\n" + 
                "</script>");

            return sb.ToString();
        }

        private static string OptionsToString(DatePickerOptions settings)
        {
            if (settings == null)
                return "changeMonth:true, changeYear:true, firstDay:1, yearRange:'-80:+10', showOn:'button', buttonImageOnly:true, buttonText:'mostrar calendario', buttonImage:'Scripts/jqueryui/images/calendar.png', constrainInput: false";

            return "changeMonth:{0}, changeYear:{1}, firstDay:{2}, yearRange:'{3}', showOn:'{4}', buttonImageOnly:{5}, buttonText:'{6}', buttonImage:'{7}', constrainInput: {8}{9}{10}".Formato(
                settings.ChangeMonth ? "true" : "false",
                settings.ChangeYear ? "true" : "false",
                settings.FirstDay,
                settings.YearRange,
                settings.ShowOn,
                settings.ButtonImageOnly ? "true" : "false",
                settings.ButtonText,
                settings.ButtonImageSrc,
                settings.ConstrainInput ? "true" : "false",
                (settings.MinDate.HasText() ? ", minDate: " + settings.MinDate : ""),
                (settings.MaxDate.HasText() ? ", maxDate: " + settings.MaxDate : "")
                );
        }
    }
}
