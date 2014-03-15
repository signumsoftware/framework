#region usings
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
#endregion

namespace Signum.Web
{
    public static class EntityBaseHelper
    {
        public static bool EmbeddedOrNew(Modifiable entity)
        {
            if (entity is EmbeddedEntity)
                return true;

            if (entity is IIdentifiable)
                return ((IIdentifiable)entity).IsNew;

            if(entity is Lite<IIdentifiable>)
                return ((Lite<IIdentifiable>)entity).IsNew;

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
            vdd[ViewDataKeys.SaveProtected] = OperationLogic.IsSaveProtected(tc.UntypedValue.GetType());
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
            ViewDataDictionary vdd;
            if (line.PreserveViewData)
            {
                vdd = helper.ViewData;
                vdd.Model = tc;
            }
            else
            {
                vdd = new ViewDataDictionary(tc);
            }
            return vdd;
        }

        private static string OnPartialViewName(TypeContext tc)
        {
            EntitySettings es = Navigator.EntitySettings(tc.UntypedValue.GetType());
           
            var result = es.OnPartialViewName((ModifiableEntity)tc.UntypedValue);
            tc.ViewOverrides = es.ViewOverrides;
            return result;
        }

        public static MvcHtmlString ViewButton(HtmlHelper helper, EntityBase entityBase)
        {
            if (!entityBase.View)
                return MvcHtmlString.Empty;

            var htmlAttr = new Dictionary<string, object>
            {
                { "onclick", new MvcHtmlString(entityBase.SFControlThen("view_click()")) },
            };

            return new HtmlTag("a", entityBase.Compose("btnView"))
                .Class("btn btn-default sf-line-button sf-view")
                .Attrs(htmlAttr)
                .Attr("title", EntityControlMessage.View.NiceToString())
                .InnerHtml(new HtmlTag("span").Class("glyphicon glyphicon-arrow-right"));
        }

        public static MvcHtmlString NavigateButton(HtmlHelper helper, EntityBase entityBase)
        {
            if (!entityBase.Navigate)
                return MvcHtmlString.Empty;

            var htmlAttr = new Dictionary<string, object>
            {
                { "onclick", new MvcHtmlString(entityBase.SFControlThen("navigate_click()")) },
            };

            return new HtmlTag("a", entityBase.Compose("btnNavigate"))
                .Class("btn btn-default sf-line-button sf-navigate")
                .Attrs(htmlAttr)
                .Attr("title", EntityControlMessage.Navigate.NiceToString())
                .InnerHtml(new HtmlTag("span").Class("glyphicon glyphicon-new-window"));
        }

        public static MvcHtmlString CreateButton(HtmlHelper helper, EntityBase entityBase)
        {
            if (!entityBase.Create)
                return MvcHtmlString.Empty;

            Type type = entityBase.Type.CleanType();

            var htmlAttr = new Dictionary<string, object>
            {
                { "onclick", entityBase.SFControlThen("create_click()") },
            };

            return new HtmlTag("a", entityBase.Compose("btnCreate"))
                .Class("btn btn-default sf-line-button sf-create")
                .Attrs(htmlAttr)
                .Attr("title", EntityControlMessage.Create.NiceToString())
                .InnerHtml(new HtmlTag("span").Class("glyphicon glyphicon-plus"));
        }

        public static MvcHtmlString FindButton(HtmlHelper helper, EntityBase entityBase)
        {
            if (!entityBase.Find)
                return MvcHtmlString.Empty;

            var htmlAttr = new Dictionary<string, object>
            {
                { "onclick", entityBase.SFControlThen("find_click()") },
            };

            return new HtmlTag("a", entityBase.Compose("btnFind"))
                .Class("btn btn-default sf-line-button sf-find")
                .Attrs(htmlAttr)
                .Attr("title", EntityControlMessage.Find.NiceToString())
                .InnerHtml(new HtmlTag("span").Class("glyphicon glyphicon-search"));
        }

        public static MvcHtmlString RemoveButton(HtmlHelper helper, EntityBase entityBase)
        {
            if (!entityBase.Remove)
                return MvcHtmlString.Empty;

            var htmlAttr = new Dictionary<string, object>
            {
                { "onclick", entityBase.SFControlThen("remove_click()") },
                { "data-icon", "ui-icon-circle-close" },
                { "data-text", false}
            };

            return new HtmlTag("a", entityBase.Compose("btnRemove"))
                .Class("btn btn-default sf-line-button sf-remove")
                .Attrs(htmlAttr)
                .Attr("title", EntityControlMessage.Remove.NiceToString())
                .InnerHtml(new HtmlTag("span").Class("glyphicon glyphicon-remove"));
        }

        static Regex regex = new Regex("(</?)script", RegexOptions.IgnoreCase);

        public static MvcHtmlString EmbeddedTemplate(EntityBase entityBase, MvcHtmlString template, string defaultString)
        {
            return MvcHtmlString.Create("<script type=\"template\" id=\"{0}\" data-toString=\"{2}\">{1}</script>".Formato(
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
                cleanType.IsEmbeddedEntity() ? Navigator.IsCreable(cleanType, isSearchEntity: false) :
                eb.Implementations.Value.IsByAll ? false :
                eb.Implementations.Value.Types.Any(t => Navigator.IsCreable(t, isSearchEntity: false));
                
            eb.View &=
                cleanType.IsEmbeddedEntity() ? Navigator.IsViewable(cleanType, eb.PartialViewName) :
                eb.Implementations.Value.IsByAll ? true :
                eb.Implementations.Value.Types.Any(t => Navigator.IsViewable(t, eb.PartialViewName));

            eb.Navigate &=
              cleanType.IsEmbeddedEntity() ? Navigator.IsNavigable(cleanType, eb.PartialViewName, isSearchEntity: false) :
              eb.Implementations.Value.IsByAll ? true :
              eb.Implementations.Value.Types.Any(t => Navigator.IsNavigable(t, eb.PartialViewName, isSearchEntity: false));

            eb.Find &=
                cleanType.IsEmbeddedEntity() ? false :
                eb.Implementations.Value.IsByAll ? false :
                eb.Implementations.Value.Types.Any(t => Navigator.IsFindable(t));
        }

        internal static MvcHtmlString ListLabel(HtmlHelper helper, BaseLine baseLine)
        {
            return new HtmlTag("label").Attr("for", baseLine.Prefix).SetInnerText(baseLine.LabelText).ToHtml();
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
}
