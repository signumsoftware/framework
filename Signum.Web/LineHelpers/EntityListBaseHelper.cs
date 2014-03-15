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
        public static MvcHtmlString MoveUpButton(HtmlHelper helper, EntityListBase listBase)
        {
            if (!listBase.Reorder)
                return MvcHtmlString.Empty;

            return new HtmlTag("a", listBase.Compose("btnUp"))
                .Class("btn btn-default sf-line-button move-up")
                .Attr("onclick", listBase.SFControlThen("moveUp_click()"))
                .Attr("title", JavascriptMessage.moveUp.NiceToString())
                .InnerHtml(new HtmlTag("span").Class("glyphicon glyphicon-chevron-up"));
        }

        public static MvcHtmlString MoveDownButton(HtmlHelper helper, EntityListBase listBase)
        {
            if (!listBase.Reorder)
                return MvcHtmlString.Empty;

            return new HtmlTag("a", listBase.Compose("btnDown"))
             .Class("btn btn-default sf-line-button move-down")
             .Attr("onclick", listBase.SFControlThen("moveDown_click()"))
             .Attr("title", JavascriptMessage.moveDown.NiceToString())
             .InnerHtml(new HtmlTag("span").Class("glyphicon glyphicon-chevron-down"));
        }

        public static MvcHtmlString WriteIndex(HtmlHelper helper, EntityListBase listBase, TypeContext itemTC, int itemIndex)
        {
            return helper.Hidden(itemTC.Compose(EntityListBaseKeys.Indexes), "{0};{1}".Formato(
                listBase.ShouldWriteOldIndex(itemTC) ? itemIndex.ToString() : "",
                itemIndex.ToString()));
        }

        public static MvcHtmlString MoveUpButtonItem(HtmlHelper helper, TypeContext itemContext, EntityListBase entityListBase, bool isVertical)
        {
            return new HtmlTag("a", itemContext.Compose("btnUp"))
                .Class("btn btn-default sf-line-button move-up")
                .Attr("onclick", entityListBase.SFControlThen("moveUp('{0}')".Formato(itemContext.Prefix)))
                .Attr("title", JavascriptMessage.moveUp.NiceToString())
                .InnerHtml(new HtmlTag("span").Class("glyphicon " + (isVertical ? "glyphicon-chevron-up" : "glyphicon-chevron-left")));
        }

        public static MvcHtmlString MoveDownButtonItem(HtmlHelper helper, TypeContext itemContext, EntityListBase entityListBase, bool isVertical)
        {
            return new HtmlTag("a", itemContext.Compose("btnDown"))
             .Class("btn btn-default sf-line-button move-down")
             .Attr("onclick", entityListBase.SFControlThen("moveDown('{0}')".Formato(itemContext.Prefix)))
             .Attr("title", JavascriptMessage.moveDown.NiceToString())
             .InnerHtml(new HtmlTag("span").Class("glyphicon " + (isVertical ? "glyphicon-chevron-down" : "glyphicon-chevron-right")));
        }

        public static MvcHtmlString RemoveButtonItem(HtmlHelper helper, TypeContext itemContext, EntityListBase entityListBase)
        {
            return new HtmlTag("a", itemContext.Compose("btnRemove"))
                  .Class("btn btn-default sf-line-button sf-remove")
                  .Attr("onclick", entityListBase.SFControlThen("removeItem_click('{0}')".Formato(itemContext.Prefix)))
                  .Attr("title", EntityControlMessage.Remove.NiceToString())
                  .InnerHtml(new HtmlTag("span").Class("glyphicon glyphicon-remove"));
        }

        public static MvcHtmlString ViewButtonItem(HtmlHelper helper, TypeContext itemContext, EntityListBase entityListBase)
        {
            return new HtmlTag("a", itemContext.Compose("btnView"))
                .Class("btn btn-default sf-line-button sf-view")
                .Attr("onclick", entityListBase.SFControlThen("viewItem_click('{0}')".Formato(itemContext.Prefix)))
                .Attr("title", EntityControlMessage.View.NiceToString())
                .InnerHtml(new HtmlTag("span").Class("glyphicon glyphicon-arrow-right"));
        }
    }
}