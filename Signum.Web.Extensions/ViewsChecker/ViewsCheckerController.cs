using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI;
using System.Threading;
using Signum.Entities.Authorization;
using Signum.Engine;
using Signum.Engine.Authorization;
using Signum.Services;
using Signum.Utilities;
using Signum.Entities;
using Signum.Web.Controllers;
using Signum.Web.Extensions.Properties;

namespace Signum.Web.ViewsChecker
{

    [HandleError]
    public class ViewsCheckerController : Controller
    {
        public ViewResult ViewsChecker()
        {
            List<ViewError> errors = new List<ViewError>();

            HtmlHelper helper = SignumController.CreateHtmlHelper(this);

            foreach (var entry in Navigator.Manager.EntitySettings)
            {
                if (!entry.Value.PartialViewName.HasText())
                    continue;

                string result = "";
                try
                {
                    result = helper.RenderPartialToString(entry.Value.PartialViewName, new ViewDataDictionary(Constructor.Construct(entry.Key, this)));
                }
                catch (Exception ex)
                {
                    int i = 0;
                }
                if (result.Contains(Resources.CompilerErrorMessage))
                {
                    ViewError error = new ViewError();
                    result = result.Substring(result.IndexOf(Resources.Description));

                    error.Description = FindRegion(result, Resources.CompilerErrorMessage);

                    error.CompilerErrorMsg = FindRegion(result, Resources.SourceCodeError);

                    error.SourceCodeError = FindRegion(result, Resources.SourceFile);

                    error.SourceFile = FindRegion(result, Resources.Line);

                    error.Line = FindRegion(result, "<br>");

                    errors.Add(error);
                }
            }

            return View("~/Plugin/Signum.Web.Extensions.dll/Signum.Web.Extensions.ViewsChecker.ViewsChecker.aspx", errors);
        }

        private string FindRegion(string result, string key)
        { 
            int index = result.IndexOf(key);
            string region = result.Substring(result.IndexOf("</b>"), index).Replace("<b>","");
            result = result.Substring(index);
            return region;
        }
    }
}
