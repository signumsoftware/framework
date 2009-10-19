using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Utilities;

namespace Signum.Web.Files
{
    public class AsyncFileUploadOptions
    { 
        
    }

    public static class AsyncFileUploadHelper
    {
        public static string AsyncFileUpload(this HtmlHelper helper, string elementId, AsyncFileUploadOptions settings)
        {
            StringBuilder sb = new StringBuilder();

            // Add Microsoft Ajax library   
            sb.AppendLine(helper.MicrosoftAjaxLibraryInclude());

            sb.AppendLine(helper.ScriptInclude("Scripts/MicrosoftAjax.debug.js",
                "Scripts/MicrosoftAjaxTimer.debug.js",
                "Scripts/MicrosoftAjaxWebForms.debug.js",
                "Scripts/MicrosoftAjaxCore.debug.js",
                "Scripts/MicrosoftAjaxSerialization.debug.js",
                "Scripts/MicrosoftAjaxNetwork.debug.js",
                "Scripts/MicrosoftAjaxComponentModel.debug.js",
                "Scripts/MicrosoftAjaxTemplates.debug.js"));

            sb.AppendLine(helper.ScriptInclude("Scripts/ACT/BaseScripts.debug.js",
                    "Scripts/ACT/Common.debug.js",
                "Scripts/ACT/AsyncFileUpload.debug.js"));

            // Perform $create   
            sb.AppendLine(helper.Create("AjaxControlToolkit.AsyncFileUpload", "", elementId));

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
