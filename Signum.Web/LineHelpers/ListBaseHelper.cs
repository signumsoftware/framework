#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Web.Mvc.Html;
using Signum.Entities;
using System.Reflection;
using Signum.Entities.Reflection;
using System.Configuration;
using Signum.Engine;
using Signum.Web.Properties;
using Signum.Utilities.Reflection;
using System.Collections;
#endregion

namespace Signum.Web
{
    public static class ListBaseHelper
    {
        public static MvcHtmlString CreateButton(HtmlHelper helper, EntityListBase listBase, Dictionary<string, object> htmlProperties)
        {
            if (!listBase.Create)
                return MvcHtmlString.Empty;

            return helper.Button(listBase.Compose("btnCreate"),
                  "+",
                  listBase.GetCreating(),
                  "lineButton create",
                  htmlProperties);
        }

        public static MvcHtmlString FindButton(HtmlHelper helper, EntityListBase listBase)
        {
            if ((!listBase.Find) || !listBase.ElementType.CleanType().IsIIdentifiable())
                return MvcHtmlString.Empty;

            return helper.Button(listBase.Compose("btnFind"),
                  "O",
                  listBase.GetFinding(),
                  "lineButton find",
                  null);
        }

        public static MvcHtmlString RemoveButton(HtmlHelper helper, EntityListBase listBase)
        {
            if (!listBase.Remove)
                return MvcHtmlString.Empty;

            IList list = (IList)listBase.UntypedValue;
            return helper.Button(listBase.Compose("btnRemove"),
                  "O",
                  listBase.GetRemoving(),
                  "lineButton remove",
                  (list == null || list.Count == 0) ? new Dictionary<string, object>() { { "style", "display:none" } } : null);
        }
    }
}