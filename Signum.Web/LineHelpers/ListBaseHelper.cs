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
using Signum.Utilities.Reflection;
using System.Collections;
#endregion

namespace Signum.Web
{
    public static class ListBaseHelper
    {
        public static MvcHtmlString MoveUpButton(HtmlHelper helper, EntityListBase listBase, bool hidden)
        {
            if (!listBase.Reorder)
                return MvcHtmlString.Empty;

            var htmlAttr = new Dictionary<string, object>
            {
                { "onclick",  listBase.SFControlThen("moveUp_click()") },
                { "data-icon", "ui-icon-triangle-1-n" },
                { "data-text", false}
            };

            if (hidden)
                htmlAttr.Add("style", "display:none");

            IList list = (IList)listBase.UntypedValue;

            if (list == null || list.Count == 0)
                htmlAttr.Add("style", "display:none");

            return helper.Href(listBase.Compose("btnUp"),
                  JavascriptMessage.moveUp.NiceToString(),
                  "",
                  JavascriptMessage.moveUp.NiceToString(),
                  "sf-line-button move-up",
                  htmlAttr);
        }

        public static MvcHtmlString MoveDownButton(HtmlHelper helper, EntityListBase listBase, bool hidden)
        {
            if (!listBase.Reorder)
                return MvcHtmlString.Empty;

            var htmlAttr = new Dictionary<string, object>
            {
                { "onclick", listBase.SFControlThen("moveDown_click()") },
                { "data-icon", "ui-icon-triangle-1-s" },
                { "data-text", false}
            };

            if (hidden)
                htmlAttr.Add("style", "display:none");

            IList list = (IList)listBase.UntypedValue;

            if (list == null || list.Count == 0)
                htmlAttr.Add("style", "display:none");

            return helper.Href(listBase.Compose("btnDown"),
                  JavascriptMessage.moveDown.NiceToString(),
                  "",
                  JavascriptMessage.moveDown.NiceToString(),
                  "sf-line-button move-down",
                  htmlAttr);
        }

        public static MvcHtmlString WriteIndex(HtmlHelper helper, EntityListBase listBase, TypeContext itemTC, int itemIndex)
        {
            return helper.Hidden(itemTC.Compose(EntityListBaseKeys.Indexes), "{0};{1}".Formato(
                listBase.ShouldWriteOldIndex(itemTC) ? itemIndex.ToString() : "", 
                itemIndex.ToString()));
        }
    }
}