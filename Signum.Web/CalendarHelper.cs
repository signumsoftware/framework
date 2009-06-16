using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace Signum.Web
{
    public static class CalendarHelper
    {
        //jQuery ui DatePicker
        public static string Calendar(this HtmlHelper helper, string elementId)
        {
            StringBuilder sb = new StringBuilder();
            string resourcesRoute = helper.ViewData[ViewDataKeys.ResourcesRoute].ToString();
            sb.Append(helper.ScriptInclude(resourcesRoute + "Scripts/jqueryui/ui.core.js"));
            sb.Append(helper.ScriptInclude(resourcesRoute + "Scripts/jqueryui/ui.datepicker.js"));
            sb.Append(helper.ScriptInclude(resourcesRoute + "Scripts/jqueryui/i18n/ui.datepicker-es.js"));

            sb.AppendLine(helper.DynamicCssInclude(resourcesRoute + "Scripts/jqueryui/ui.all.css"));
            sb.AppendLine(helper.DynamicCssInclude(resourcesRoute + "Scripts/jqueryui/ui.base.css"));
            sb.AppendLine(helper.DynamicCssInclude(resourcesRoute + "Scripts/jqueryui/ui.core.css"));
            sb.AppendLine(helper.DynamicCssInclude(resourcesRoute + "Scripts/jqueryui/ui.datepicker.css"));
            sb.AppendLine(helper.DynamicCssInclude(resourcesRoute + "Scripts/jqueryui/ui.theme.css"));

            sb.Append(
                "<script type=\"text/javascript\">\n" + 
                "$(document).ready(function(){\n" + 
                "$(\"#" + elementId + "\").datepicker($.datepicker.regional['es']);\n" + 
                "});\n" + 
                "</script>\n");

            return sb.ToString();
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
