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

            Type type = listBase.ElementType.CleanType();

            var htmlAttr = new Dictionary<string, object>
            {
                { "onclick", listBase.GetCreating() },
                { "data-icon", "ui-icon-circle-plus" },
                { "data-text", false}
            };

            if (htmlProperties != null)
                htmlAttr.AddRange(htmlProperties);

            return helper.Href(listBase.Compose("btnCreate"),
                  Resources.LineButton_Create,
                  "",
                  Resources.LineButton_Create,
                  "sf-line-button sf-create",
                  htmlAttr);
        }

        public static MvcHtmlString ViewButton(HtmlHelper helper, EntityListBase listBase)
        {
            if (!listBase.View)
                return MvcHtmlString.Empty;

            var htmlAttr = new Dictionary<string, object>
            {
                { "onclick", listBase.GetViewing() },
                { "data-icon", "ui-icon-circle-arrow-e" },
                { "data-text", false}
            };

            return helper.Href(listBase.Compose("btnView"),
                  Resources.LineButton_View,
                  "",
                  Resources.LineButton_View,
                  "sf-line-button sf-view",
                  htmlAttr);
        }

        public static MvcHtmlString NavigateButton(HtmlHelper helper, EntityListBase listBase)
        {
            if (!listBase.View)
                return MvcHtmlString.Empty;

            var htmlAttr = new Dictionary<string, object>
            {
                { "onclick", listBase.GetViewing() },
                { "data-icon", "ui-icon-arrowthick-1-e" },
                { "data-text", false}
            };

            return helper.Href(listBase.Compose("btnNavigate"),
                  Resources.LineButton_Navigate,
                  "",
                  Resources.LineButton_Navigate,
                  "sf-line-button sf-navigate",
                  htmlAttr);
        }

        public static MvcHtmlString FindButton(HtmlHelper helper, EntityListBase listBase)
        {
            if (!listBase.Find)
                return MvcHtmlString.Empty;

            var htmlAttr = new Dictionary<string, object>
            {
                { "onclick", listBase.GetFinding() },
                { "data-icon", "ui-icon-circle-zoomin" },
                { "data-text", false}
            };

            return helper.Href(listBase.Compose("btnFind"),
                  Resources.LineButton_Find,
                  "",
                  Resources.LineButton_Find,
                  "sf-line-button sf-find",
                  htmlAttr);
        }

        public static MvcHtmlString RemoveButton(HtmlHelper helper, EntityListBase listBase)
        {
            if (!listBase.Remove)
                return MvcHtmlString.Empty;

            var htmlAttr = new Dictionary<string, object>
            {
                { "onclick", listBase.GetRemoving() },
                { "data-icon", "ui-icon-circle-close" },
                { "data-text", false}
            };

            IList list = (IList)listBase.UntypedValue;

            if (list == null || list.Count == 0)
                htmlAttr.Add("style", "display:none");

            return helper.Href(listBase.Compose("btnRemove"),
                  Resources.LineButton_Remove,
                  "",
                  Resources.LineButton_Remove,
                  "sf-line-button sf-remove",
                  htmlAttr);
        }

        public static MvcHtmlString MoveUpButton(HtmlHelper helper, EntityListBase listBase)
        {
            if (!listBase.Reorder)
                return MvcHtmlString.Empty;

            var htmlAttr = new Dictionary<string, object>
            {
                { "onclick", listBase.GetMovingUp() },
                { "data-icon", "ui-icon-triangle-1-n" },
                { "data-text", false}
            };

            IList list = (IList)listBase.UntypedValue;

            if (list == null || list.Count == 0)
                htmlAttr.Add("style", "display:none");

            return helper.Href(listBase.Compose("btnUp"),
                  Resources.Signum_entityRepeater_moveUp,
                  "",
                  Resources.Signum_entityRepeater_moveUp,
                  "sf-line-button move-up",
                  htmlAttr);
        }

        public static MvcHtmlString MoveDownButton(HtmlHelper helper, EntityListBase listBase)
        {
            if (!listBase.Reorder)
                return MvcHtmlString.Empty;

            var htmlAttr = new Dictionary<string, object>
            {
                { "onclick", listBase.GetMovingDown() },
                { "data-icon", "ui-icon-triangle-1-s" },
                { "data-text", false}
            };

            IList list = (IList)listBase.UntypedValue;

            if (list == null || list.Count == 0)
                htmlAttr.Add("style", "display:none");

            return helper.Href(listBase.Compose("btnDown"),
                  Resources.Signum_entityRepeater_moveDown,
                  "",
                  Resources.Signum_entityRepeater_moveDown,
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