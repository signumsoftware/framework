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

            string queryUrlName = (string)bindingContext.ValueProvider.TryGetC("queryUrlName").TryCC(vp => vp.RawValue)
                                  .ThrowIfNullC(Resources.QueryUrlNameWasNotProvided);
            fo.QueryName = Navigator.ResolveQueryFromUrlName(queryUrlName);

            fo.FilterOptions = Navigator.ExtractFilterOptions(controllerContext.HttpContext, fo.QueryName);

            if (parameters.AllKeys.Any(k => k == "allowMultiple"))
                fo.AllowMultiple = bool.Parse(parameters["allowMultiple"]);

            if (parameters.AllKeys.Any(k => k == "searchOnLoad"))
                fo.SearchOnLoad = bool.Parse(parameters["searchOnLoad"]);

            return fo;
        }
    }
}
