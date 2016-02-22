using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using Signum.Utilities;
using Signum.Entities;
using Signum.Web.Controllers;
using Signum.Entities.Reflection;
using Signum.Engine.Operations;
using System.Text.RegularExpressions;

namespace Signum.Web
{
    public static class EntityBaseHelper
    {
        public static bool EmbeddedOrNew(Modifiable entity)
        {
            if (entity is EmbeddedEntity)
                return true;

            if (entity is IEntity)
                return ((IEntity)entity).IsNew;

            if(entity is Lite<IEntity>)
                return ((Lite<IEntity>)entity).IsNew;

            return false;
        }

        public static MvcHtmlString RenderPopup(HtmlHelper helper, TypeContext typeContext, RenderPopupMode mode, EntityBase line, bool isTemplate = false)
        {
            TypeContext tc = TypeContextUtilities.CleanTypeContext((TypeContext)typeContext);

            ViewDataDictionary vdd = GetViewData(helper, line, tc);

            string partialViewName = line.PartialViewName ?? OnPartialViewName(tc);

            vdd[ViewDataKeys.PartialViewName] = partialViewName;
            vdd[ViewDataKeys.ViewMode] = !line.ReadOnly;
            vdd[ViewDataKeys.ViewMode] = ViewMode.View;
            vdd[ViewDataKeys.ShowOperations] = true;
            vdd[ViewDataKeys.RequiresSaveOperation] = tc.UntypedValue is Entity && EntityKindCache.RequiresSaveOperation(tc.UntypedValue.GetType());
            vdd[ViewDataKeys.WriteEntityState] = 
                !isTemplate &&
                !(tc.UntypedValue is EmbeddedEntity) &&
                ((ModifiableEntity)tc.UntypedValue).Modified == ModifiedState.SelfModified;

            switch (mode)
            {
                case RenderPopupMode.Popup:
                    return helper.Partial(Navigator.Manager.PopupControlView, vdd);
                case RenderPopupMode.PopupInDiv:
                    return helper.Div(typeContext.Compose(EntityBaseKeys.Entity),
                        helper.Partial(Navigator.Manager.PopupControlView, vdd),  
                        "",
                        new Dictionary<string, object> { { "style", "display:none" } });
                default:
                    throw new InvalidOperationException();
            }
        }
        

        public static MvcHtmlString RenderContent(HtmlHelper helper, TypeContext typeContext, RenderContentMode mode, EntityBase line)
        {
            TypeContext tc = TypeContextUtilities.CleanTypeContext((TypeContext)typeContext);

            ViewDataDictionary vdd = GetViewData(helper, line, tc);
            
            string partialViewName = line.PartialViewName ?? OnPartialViewName(tc);

            switch (mode)
            {
                case RenderContentMode.Content:
                    return helper.Partial(partialViewName, vdd);

                case RenderContentMode.ContentInVisibleDiv:
                    return helper.Div(typeContext.Compose(EntityBaseKeys.Entity),
                      helper.Partial(partialViewName, vdd), "",
                      null);
                case RenderContentMode.ContentInInvisibleDiv:
                    return helper.Div(typeContext.Compose(EntityBaseKeys.Entity),
                        helper.Partial(partialViewName, vdd), "",
                         new Dictionary<string, object> { { "style", "display:none" } });
                default:
                    throw new InvalidOperationException();
            }
        }

        private static ViewDataDictionary GetViewData(HtmlHelper helper, EntityBase line, TypeContext tc)
        {
            ViewDataDictionary vdd = new ViewDataDictionary(tc);
            
            if (line.PreserveViewData)
                vdd.AddRange(helper.ViewData);
            
            return vdd;
        }

        private static string OnPartialViewName(TypeContext tc)
        {
            EntitySettings es = Navigator.EntitySettings(tc.UntypedValue.GetType());
           
            var result = es.OnPartialViewName((ModifiableEntity)tc.UntypedValue);
            tc.ViewOverrides = es.GetViewOverrides();
            return result;
        }

    

        public static MvcHtmlString WriteIndex<T>(HtmlHelper helper, TypeElementContext<T> itemTC)
        {
            return helper.Hidden(itemTC.Compose(EntityListBaseKeys.Index), itemTC.Index).Concat(
                   helper.Hidden(itemTC.Compose(EntityListBaseKeys.RowId), itemTC.RowId));
        }

        static Regex regex = new Regex("(</?)script", RegexOptions.IgnoreCase);

        public static MvcHtmlString EmbeddedTemplate(EntityBase entityBase, MvcHtmlString template, string defaultString)
        {
            return MvcHtmlString.Create("<script type=\"template\" id=\"{0}\" data-toString=\"{2}\">{1}</script>".FormatWith(
                                entityBase.Compose(EntityBaseKeys.Template),
                                regex.Replace(template.ToHtmlString(), m => m.Value + "X"),
                                defaultString));
        }

        public static void ConfigureEntityBase(EntityBase eb, Type cleanType)
        {
            Common.TaskSetImplementations(eb);

            ConfigureEntityButtons(eb, cleanType);
        }

        public static void ConfigureEntityButtons(EntityBase eb, Type cleanType)
        {
           eb.Create &= 
                cleanType.IsEmbeddedEntity() ? Navigator.IsCreable(cleanType, isSearch: false) :
                eb.Implementations.Value.IsByAll ? false :
                eb.Implementations.Value.Types.Any(t => Navigator.IsCreable(t, isSearch: false));
                
            eb.View &=
                cleanType.IsEmbeddedEntity() ? Navigator.IsViewable(cleanType, eb.PartialViewName) :
                eb.Implementations.Value.IsByAll ? true :
                eb.Implementations.Value.Types.Any(t => Navigator.IsViewable(t, eb.PartialViewName));

            eb.Navigate &=
              cleanType.IsEmbeddedEntity() ? Navigator.IsNavigable(cleanType, eb.PartialViewName, isSearch: false) :
              eb.Implementations.Value.IsByAll ? true :
              eb.Implementations.Value.Types.Any(t => Navigator.IsNavigable(t, eb.PartialViewName, isSearch: false));

            eb.Find &=
                cleanType.IsEmbeddedEntity() ? false :
                eb.Implementations.Value.IsByAll ? false :
                eb.Implementations.Value.Types.Any(t => Navigator.IsFindable(t));
        }
    }

    public enum RenderContentMode
    {
        Content,
        ContentInVisibleDiv,
        ContentInInvisibleDiv
    }


    public enum RenderPopupMode
    {
        Popup,
        PopupInDiv,
    }

    public static class EntityButtonHelper
    {
        public static MvcHtmlString View(HtmlHelper helper, EntityBase entityBase, bool btn)
        {
            if (!entityBase.View)
                return MvcHtmlString.Empty;

            return new HtmlTag("a", entityBase.Compose("btnView"))
                .Class(btn ? "btn btn-default" : null)
                .Class("sf-line-button sf-view")
                .Attr("onclick", entityBase.SFControlThen("view_click(event)"))
                .Attr("title", EntityControlMessage.View.NiceToString())
                .InnerHtml(new HtmlTag("span").Class("glyphicon glyphicon-arrow-right"));
        }

        public static MvcHtmlString ViewItem(HtmlHelper helper, TypeContext itemContext, EntityListBase entityListBase, bool btn, string elementType = "a")
        {
            return new HtmlTag(elementType, itemContext.Compose("btnView"))
                .Class(btn ? "btn btn-default" : null)
                .Class("sf-line-button sf-view")
                .Attr("onclick", entityListBase.SFControlThen("viewItem_click('{0}', event)".FormatWith(itemContext.Prefix)))
                .Attr("title", EntityControlMessage.View.NiceToString())
                .InnerHtml(new HtmlTag("span").Class("glyphicon glyphicon-arrow-right"));
        }


        public static MvcHtmlString Create(HtmlHelper helper, EntityBase entityBase, bool btn)
        {
            if (!entityBase.Create)
                return MvcHtmlString.Empty;

            return new HtmlTag("a", entityBase.Compose("btnCreate"))
                .Class(btn ? "btn btn-default" : null)
                .Class("sf-line-button sf-create")
                .Attr("onclick", entityBase.SFControlThen("create_click(event)"))
                .Attr("title", EntityControlMessage.Create.NiceToString())
                .InnerHtml(new HtmlTag("span").Class("glyphicon glyphicon-plus"));
        }



        public static MvcHtmlString Find(HtmlHelper helper, EntityBase entityBase, bool btn)
        {
            if (!entityBase.Find)
                return MvcHtmlString.Empty;

            return new HtmlTag("a", entityBase.Compose("btnFind"))
                .Class(btn ? "btn btn-default" : null)
                .Class("sf-line-button sf-find")
                .Attr("onclick", entityBase.SFControlThen("find_click(event)"))
                .Attr("title", EntityControlMessage.Find.NiceToString())
                .InnerHtml(new HtmlTag("span").Class("glyphicon glyphicon-search"));
        }


        public static MvcHtmlString Remove(HtmlHelper helper, EntityBase entityBase, bool btn)
        {
            if (!entityBase.Remove)
                return MvcHtmlString.Empty;

            return new HtmlTag("a", entityBase.Compose("btnRemove"))
                .Class(btn ? "btn btn-default" : null)
                .Class("sf-line-button sf-remove")
                .Attr("onclick", entityBase.SFControlThen("remove_click(event)"))
                .Attr("title", EntityControlMessage.Remove.NiceToString())
                .InnerHtml(new HtmlTag("span").Class("glyphicon glyphicon-remove"));
        }

        public static MvcHtmlString RemoveItem(HtmlHelper helper, TypeContext itemContext, EntityListBase entityListBase, bool btn, string elementType = "a")
        {
            return new HtmlTag(elementType, itemContext.Compose("btnRemove"))
                  .Class(btn ? "btn btn-default" : null)
                  .Class("sf-line-button sf-remove")
                  .Attr("onclick", entityListBase.SFControlThen("removeItem_click('{0}', event)".FormatWith(itemContext.Prefix)))
                  .Attr("title", EntityControlMessage.Remove.NiceToString())
                  .InnerHtml(new HtmlTag("span").Class("glyphicon glyphicon-remove"));
        }


        public static MvcHtmlString MoveUp(HtmlHelper helper, EntityListBase listBase, bool btn)
        {
            if (!listBase.Move)
                return MvcHtmlString.Empty;

            return new HtmlTag("a", listBase.Compose("btnUp"))
                .Class(btn ? "btn btn-default" : null)
                .Class("sf-line-button move-up")
                .Attr("onclick", listBase.SFControlThen("moveUp_click(event)"))
                .Attr("title", JavascriptMessage.moveUp.NiceToString())
                .InnerHtml(new HtmlTag("span").Class("glyphicon glyphicon-chevron-up"));
        }

        public static MvcHtmlString MoveUpItem(HtmlHelper helper, TypeContext itemContext, EntityListBase entityListBase, bool btn, string elementType = "a", bool isVertical = true)
        {
            return new HtmlTag(elementType, itemContext.Compose("btnUp"))
                .Class(btn ? "btn btn-default" : null)
                .Class("sf-line-button move-up") 
                .Attr("onclick", entityListBase.SFControlThen("moveUp('{0}', event)".FormatWith(itemContext.Prefix)))
                .Attr("title", JavascriptMessage.moveUp.NiceToString())
                .InnerHtml(new HtmlTag("span").Class("glyphicon " + (isVertical ? "glyphicon-chevron-up" : "glyphicon-chevron-left")));
        }



        public static MvcHtmlString MoveDown(HtmlHelper helper, EntityListBase listBase, bool btn)
        {
            if (!listBase.Move)
                return MvcHtmlString.Empty;

            return new HtmlTag("a", listBase.Compose("btnDown"))
             .Class(btn ? "btn btn-default" : null)
             .Class("sf-line-button move-down")
             .Attr("onclick", listBase.SFControlThen("moveDown_click(event)"))
             .Attr("title", JavascriptMessage.moveDown.NiceToString())
             .InnerHtml(new HtmlTag("span").Class("glyphicon glyphicon-chevron-down"));
        }

        public static MvcHtmlString MoveDownItem(HtmlHelper helper, TypeContext itemContext, EntityListBase entityListBase, bool btn, string elementType = "a", bool isVertical = true)
        {
            return new HtmlTag(elementType, itemContext.Compose("btnDown"))
             .Class(btn ? "btn btn-default" : null)
             .Class("sf-line-button move-down")
             .Attr("onclick", entityListBase.SFControlThen("moveDown('{0}', event)".FormatWith(itemContext.Prefix)))
             .Attr("title", JavascriptMessage.moveDown.NiceToString())
             .InnerHtml(new HtmlTag("span").Class("glyphicon " + (isVertical ? "glyphicon-chevron-down" : "glyphicon-chevron-right")));
        }

      
    }
}
