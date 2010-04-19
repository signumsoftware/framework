using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Utilities;


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

        string buttonText = "Mostrar calendario";
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
    }

    public static class CalendarHelper
    {
        public static Action<HtmlHelper, StringBuilder> IncludeCss;
        public static string jQueryPrefix = "";
        //jQuery ui DatePicker
        public static string Calendar(this HtmlHelper helper, string elementId, DatePickerOptions settings)
        {
            StringBuilder sb = new StringBuilder();
            //sb.Append(helper.ScriptInclude(helper.CombinedJsUrlPath("Scripts/jqueryui", "ui.core.js", "ui.datepicker.js", "i18n/ui.datepicker-es.js")));
            sb.AppendLine(helper.ScriptInclude("Scripts/jqueryui/" + jQueryPrefix + "ui.core.js",
                "Scripts/jqueryui/"+ jQueryPrefix + "ui.datepicker.js",
                "Scripts/jqueryui/i18n/" + jQueryPrefix + "ui.datepicker-es.js"));

            //sb.AppendLine(helper.DynamicCssInclude(helper.CombinedCssUrlPath("Scripts/jqueryui", "ui.all.css", "ui.base.css", "ui.core.css", "ui.datepicker.css", "ui.theme.css")));

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
                return "changeMonth:true, changeYear:true, firstDay:1, yearRange:'c-90:c+10', showOn:'button', buttonImageOnly:true, buttonText:'mostrar calendario', buttonImage:'Scripts/jqueryui/images/calendar.png'";

            return "changeMonth:{0}, changeYear:{1}, firstDay:{2}, yearRange:'{3}', showOn:'{4}', buttonImageOnly:{5}, buttonText:'{6}', buttonImage:'{7}'{8}{9}".Formato(
                settings.ChangeMonth ? "true" : "false",
                settings.ChangeYear ? "true" : "false",
                settings.FirstDay,
                settings.YearRange,
                settings.ShowOn,
                settings.ButtonImageOnly ? "true" : "false",
                settings.ButtonText,
                settings.ButtonImageSrc,
                (settings.MinDate.HasText() ? ", minDate: " + settings.MinDate : ""),
                (settings.MaxDate.HasText() ? ", maxDate: " + settings.MaxDate : "")
                );
        }

        //Ajax control toolkit calendar
        public static string CalendarAjaxControlToolkit(this HtmlHelper helper, string elementId)
        {
            var sb = new StringBuilder();

            // Add Microsoft Ajax library   
            sb.AppendLine(helper.MicrosoftAjaxLibraryInclude());

            // Add toolkit scripts   
            sb.AppendLine(helper.ToolkitInclude
                (
                    "AjaxControlToolkit.ExtenderBase.BaseScripts.js",
                    "AjaxControlToolkit.Common.Common.js",
                    "AjaxControlToolkit.Common.DateTime.js",
                    "AjaxControlToolkit.Animation.Animations.js",
                    "AjaxControlToolkit.PopupExtender.PopupBehavior.js",
                    "AjaxControlToolkit.Animation.AnimationBehavior.js",
                    "AjaxControlToolkit.Common.Threading.js",
                    "AjaxControlToolkit.Compat.Timer.Timer.js",
                    "AjaxControlToolkit.Calendar.CalendarBehavior.js"
                ));

            // Add Calendar CSS file   
            sb.AppendLine(helper.DynamicToolkitCssInclude("AjaxControlToolkit.Calendar.Calendar.css"));

            // Perform $create   
            sb.AppendLine(helper.Create("AjaxControlToolkit.CalendarBehavior", "{\"format\":\"dd/MM/yyyy\"}", elementId));

            return sb.ToString();
        }
    }
}
