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

        public static MvcHtmlString MoveUpSpanItem(HtmlHelper helper, TypeContext itemContext, EntityListBase entityListBase, string elementType = "a", bool isVertical = true)
        {
            return new HtmlTag(elementType, itemContext.Compose("btnUp"))
                .Class("sf-line-button move-up")
                .Attr("onclick", entityListBase.SFControlThen("moveUp('{0}')".Formato(itemContext.Prefix)))
                .Attr("title", JavascriptMessage.moveUp.NiceToString())
                .InnerHtml(new HtmlTag("span").Class("glyphicon " + (isVertical ? "glyphicon-chevron-up" : "glyphicon-chevron-left")));
        }

        public static MvcHtmlString MoveDownSpanItem(HtmlHelper helper, TypeContext itemContext, EntityListBase entityListBase, string elementType = "a", bool isVertical = true)
        {
            return new HtmlTag(elementType, itemContext.Compose("btnDown"))
             .Class("sf-line-button move-down")
             .Attr("onclick", entityListBase.SFControlThen("moveDown('{0}')".Formato(itemContext.Prefix)))
             .Attr("title", JavascriptMessage.moveDown.NiceToString())
             .InnerHtml(new HtmlTag("span").Class("glyphicon " + (isVertical ? "glyphicon-chevron-down" : "glyphicon-chevron-right")));
        }

        public static MvcHtmlString RemoveSpanItem(HtmlHelper helper, TypeContext itemContext, EntityListBase entityListBase, string elementType = "a")
        {
            return new HtmlTag(elementType, itemContext.Compose("btnRemove"))
                  .Class("sf-line-button sf-remove")
                  .Attr("onclick", entityListBase.SFControlThen("removeItem_click('{0}')".Formato(itemContext.Prefix)))
                  .Attr("title", EntityControlMessage.Remove.NiceToString())
                  .InnerHtml(new HtmlTag("span").Class("glyphicon glyphicon-remove"));
        }

        public static MvcHtmlString ViewSpanItem(HtmlHelper helper, TypeContext itemContext, EntityListBase entityListBase, string elementType = "a")
        {
            return new HtmlTag(elementType, itemContext.Compose("btnView"))
                .Class("sf-line-button sf-view")
                .Attr("onclick", entityListBase.SFControlThen("viewItem_click('{0}')".Formato(itemContext.Prefix)))
                .Attr("title", EntityControlMessage.View.NiceToString())
                .InnerHtml(new HtmlTag("span").Class("glyphicon glyphicon-arrow-right"));
        }


        public static MvcHtmlString CreateSpan(HtmlHelper helper, EntityBase entityBase)
        {
            if (!entityBase.Create)
                return MvcHtmlString.Empty;

            Type type = entityBase.Type.CleanType();

            return new HtmlTag("a", entityBase.Compose("btnCreate"))
                .Class("sf-line-button sf-create")
                .Attr("onclick", entityBase.SFControlThen("create_click()"))
                .Attr("title", EntityControlMessage.Create.NiceToString())
                .InnerHtml(new HtmlTag("span").Class("glyphicon glyphicon-plus"));
        }

        public static MvcHtmlString FindSpan(HtmlHelper helper, EntityBase entityBase)
        {
            if (!entityBase.Find)
                return MvcHtmlString.Empty;

            return new HtmlTag("a", entityBase.Compose("btnFind"))
                .Class("sf-line-button sf-find")
                .Attr("onclick", entityBase.SFControlThen("find_click()"))
                .Attr("title", EntityControlMessage.Find.NiceToString())
                .InnerHtml(new HtmlTag("span").Class("glyphicon glyphicon-search"));
        }

        public static MvcHtmlString RemoveSpan(HtmlHelper helper, EntityBase entityBase)
        {
            if (!entityBase.Find)
                return MvcHtmlString.Empty;

            return new HtmlTag("a", entityBase.Compose("btnRemove"))
                .Class("sf-line-button sf-remove")
                .Attr("onclick", entityBase.SFControlThen("remove_click()"))
                .Attr("title", EntityControlMessage.Remove.NiceToString())
                .InnerHtml(new HtmlTag("span").Class("glyphicon glyphicon-remove"));
        }
    }
}