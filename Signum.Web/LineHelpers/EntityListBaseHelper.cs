using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Signum.Entities;
using Signum.Utilities;
using System.Web.Mvc.Html;

namespace Signum.Web
{
    public static class EntityListBaseHelper
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

        public static MvcHtmlString MoveUpButtonItem(HtmlHelper helper, TypeContext itemContext, EntityListBase entityListBase, bool isVertical)
        {
            return helper.Span(itemContext.Compose("btnUp"),
                JavascriptMessage.moveUp.NiceToString(),
                "sf-line-button sf-move-up",
                new Dictionary<string, object> 
                {  
                   { "onclick", entityListBase.SFControlThen("moveUp('{0}')".Formato(itemContext.Prefix)) },
                   { "data-icon",  "ui-icon-triangle-1-" + (isVertical ? "n" : "w") },
                   { "data-text", false },
                   { "title", JavascriptMessage.moveUp.NiceToString() }
                });
        }

        public static MvcHtmlString MoveDownButtonItem(HtmlHelper helper, TypeContext itemContext, EntityListBase entityListBase, bool isVertical)
        {
            return helper.Span(itemContext.Compose("btnDown"),
                JavascriptMessage.moveDown.NiceToString(),
                "sf-line-button sf-move-down",
                new Dictionary<string, object> 
                 {   
                    { "onclick", entityListBase.SFControlThen("moveDown('{0}')".Formato(itemContext.Prefix)) },
                    { "data-icon", "ui-icon-triangle-1-s" },
                    { "data-text", false },
                    { "title", JavascriptMessage.moveDown.NiceToString() }
                 });
        }

        public static MvcHtmlString RemoveButtonItem(HtmlHelper helper, TypeContext itemContext, EntityListBase entityListBase)
        {
            return helper.Href(itemContext.Compose("btnRemove"),
                EntityControlMessage.Remove.NiceToString(),
                "",
                EntityControlMessage.Remove.NiceToString(),
                "sf-line-button sf-remove",
                new Dictionary<string, object> 
                {
                    { "onclick", entityListBase.SFControlThen("removeItem_click('{0}')".Formato(itemContext.Prefix)) },
                    { "data-icon", "ui-icon-circle-close" }, 
                    { "data-text", false } 
                });
        }

        public static MvcHtmlString ViewButtonItem(HtmlHelper helper, TypeContext itemContext, EntityListBase entityListBase)
        {
            return helper.Href(itemContext.Compose("btnView"),
                EntityControlMessage.View.NiceToString(),
                "",
                EntityControlMessage.View.NiceToString(),
                "sf-line-button sf-view",
                new Dictionary<string, object> 
                {
                    { "onclick", entityListBase.SFControlThen("viewItem_click('{0}')".Formato(itemContext.Prefix)) },
                    { "data-icon",  "ui-icon-circle-arrow-e" },
                    { "data-text", false } 
                });
        }
    }
}