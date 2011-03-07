using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
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
    [HandleException]
    public class ViewsCheckerController : Controller
    {
        public ViewResult Index()
        {
            //We ensure Buffer is active
            Response.Buffer = true;

            List<ViewError> errors = new List<ViewError>();

            HtmlHelper helper = SignumController.CreateHtmlHelper(this);

            foreach (var entry in Navigator.Manager.EntitySettings)
            {
                ModifiableEntity entity = Constructor.Construct(entry.Key);
                try
                {
                    entry.Value.OnPartialViewName(entity);
                }
                catch(Exception)
                {
                    continue; //If it doesn't have an associated file, continue with the next type
                }

                MvcHtmlString result = null;
                try
                {
                    Response.Clear();
                    entity = (ModifiableEntity)Constructor.Construct(entry.Key);
                    result = helper.Partial(entry.Value.OnPartialViewName(entity), new ViewDataDictionary(entity));
                }
                catch (Exception ex)
                {
                    Exception firstEx = FindMostInnerException(ex);

                    ViewError error = new ViewError
                    {
                        ViewName = entry.Value.OnPartialViewName(entity),
                        Message = ex.Message,
                        Source = ex.Source,
                        StackTrace = ex.StackTrace,
                        TargetSite = ex.TargetSite.ToString()
                    };

                    errors.Add(error);
                }
            }
            //Clear content written by the renderization of views, just want error content
            Response.Clear();

            return View("ViewsChecker", errors);
        }

        private string FindRegion(string result, string key)
        { 
            int index = result.IndexOf(key);
            string region = result.Substring(result.IndexOf("</b>"), index).Replace("<b>","");
            result = result.Substring(index);
            return region;
        }

        private Exception FindMostInnerException(Exception ex)
        {
            if (ex.InnerException == null)
                return ex;

            return FindMostInnerException(ex.InnerException);
        }
    }
}
