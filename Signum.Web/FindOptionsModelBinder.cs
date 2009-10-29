using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Collections.Specialized;
using Signum.Utilities;
using Signum.Entities.DynamicQuery;
using Signum.Web.Properties;

namespace Signum.Web
{
    public class FindOptionsModelBinder : IModelBinder
    {
        public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            FindOptions fo = new FindOptions();

            NameValueCollection parameters = controllerContext.HttpContext.Request.Params;

            string queryUrlName = "";
            object rawValue = bindingContext.ValueProvider.TryGetC("sfQueryUrlName").TryCC(vp => vp.RawValue);
            if (rawValue.GetType() == typeof(string[]))
                queryUrlName = ((string[])rawValue)[0];
            else 
                queryUrlName = (string)rawValue;

            if (!queryUrlName.HasText())
                 throw new ApplicationException(Resources.QueryUrlNameWasNotProvided);

            fo.QueryName = Navigator.ResolveQueryFromUrlName(queryUrlName);

            fo.FilterOptions = Navigator.ExtractFilterOptions(controllerContext.HttpContext, fo.QueryName);

            if (parameters.AllKeys.Any(k => k == "sfAllowMultiple"))
                fo.AllowMultiple = bool.Parse(parameters["sfAllowMultiple"]);

            if (parameters.AllKeys.Any(k => k == "sfSearchOnLoad"))
                fo.SearchOnLoad = bool.Parse(parameters["sfSearchOnLoad"]);

            return fo;
        }
    }
}
