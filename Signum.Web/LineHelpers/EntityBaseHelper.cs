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
#endregion

namespace Signum.Web
{
    public static class EntityBaseHelper
    {
        public static MvcHtmlString BaseLineLabel(HtmlHelper helper, BaseLine baseLine)
        {
            return BaseLineLabel(helper, baseLine, baseLine.Compose(EntityBaseKeys.ToStr));
        }

        public static MvcHtmlString BaseLineLabel(HtmlHelper helper, BaseLine baseLine, string idLabelFor)
        {
            return baseLine.LabelVisible && !baseLine.OnlyValue ?
                   helper.Label(baseLine.Compose("lbl"), baseLine.LabelText ?? "", idLabelFor, baseLine.LabelClass) :
                   MvcHtmlString.Empty;
        }

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

        public static string JsEscape(string input)
        {
            return input.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("/", "\\/").Replace("\r\n", "").Replace("\n", "");
        }

        public static MvcHtmlString ViewButton(HtmlHelper helper, EntityBase entityBase, bool hidden)
        {
            if (!entityBase.View)
                return MvcHtmlString.Empty;

            var htmlAttr = new Dictionary<string, object>
            {
                { "onclick", new MvcHtmlString("{0}.view_click()".Formato(entityBase.SFControl())) },
                { "data-icon",  "ui-icon-circle-arrow-e" },
                { "data-text", false}
            };

            if (hidden)
                htmlAttr.Add("style", "display:none");

            return helper.Href(entityBase.Compose("btnView"),
                  EntityControlMessage.View.NiceToString(),
                  "",
                  EntityControlMessage.View.NiceToString(),
                  "sf-line-button sf-view",
                  htmlAttr);
        }

        public static MvcHtmlString NavigateButton(HtmlHelper helper, EntityBase entityBase)
        {
            if (!entityBase.Navigate)
                return MvcHtmlString.Empty;

            var htmlAttr = new Dictionary<string, object>
            {
                { "onclick", new MvcHtmlString("{0}.view_click()".Formato(entityBase.SFControl())) },
                { "data-icon", "ui-icon-arrowthick-1-e" },
                { "data-text", false}
            };

            if (entityBase.UntypedValue == null)
                htmlAttr.Add("style", "display:none");

            return helper.Href(entityBase.Compose("btnView"),
                  EntityControlMessage.Navigate.NiceToString(),
                  "",
                  EntityControlMessage.Navigate.NiceToString(),
                  "sf-line-button sf-view",
                  htmlAttr);
        }

        public static MvcHtmlString CreateButton(HtmlHelper helper, EntityBase entityBase, bool hidden)
        {
            if (!entityBase.Create)
                return MvcHtmlString.Empty;

            Type type = entityBase.Type.CleanType();

            var htmlAttr = new Dictionary<string, object>
            {
                { "onclick", "{0}.create_click()".Formato(entityBase.SFControl()) },
                { "data-icon", "ui-icon-circle-plus" },
                { "data-text", false}
            };

            if (hidden)
                htmlAttr.Add("style", "display:none");

            return helper.Href(entityBase.Compose("btnCreate"),
                  EntityControlMessage.Create.NiceToString(),
                  "",
                  EntityControlMessage.Create.NiceToString(),
                  "sf-line-button sf-create",
                  htmlAttr);
        }

        public static MvcHtmlString FindButton(HtmlHelper helper, EntityBase entityBase, bool hidden)
        {
            if (!entityBase.Find)
                return MvcHtmlString.Empty;

            var htmlAttr = new Dictionary<string, object>
            {
                { "onclick", "{0}.find_click()".Formato(entityBase.SFControl()) },
                { "data-icon", "ui-icon-circle-zoomin" },
                { "data-text", false}
            };

            if (hidden)
                htmlAttr.Add("style", "display:none");

            return helper.Href(entityBase.Compose("btnFind"),
                  EntityControlMessage.Find.NiceToString(),
                  "",
                  EntityControlMessage.Find.NiceToString(),
                  "sf-line-button sf-find",
                  htmlAttr);
        }

        public static MvcHtmlString RemoveButton(HtmlHelper helper, EntityBase entityBase, bool hidden)
        {
            if (!entityBase.Remove)
                return MvcHtmlString.Empty;

            var htmlAttr = new Dictionary<string, object>
            {
                { "onclick", "{0}.remove_click()".Formato(entityBase.SFControl()) },
                { "data-icon", "ui-icon-circle-close" },
                { "data-text", false}
            };

            if (hidden)
                htmlAttr.Add("style", "display:none");

            return helper.Href(entityBase.Compose("btnRemove"),
                  EntityControlMessage.Remove.NiceToString(),
                  "",
                  EntityControlMessage.Remove.NiceToString(),
                  "sf-line-button sf-remove",
                  htmlAttr);
        }

        public static MvcHtmlString EmbeddedTemplate(EntityBase entityBase, MvcHtmlString template)
        {
            return MvcHtmlString.Create("<script type=\"text/javascript\">var {0} = \"{1}\"</script>".Formato(
                                entityBase.Compose(EntityBaseKeys.Template),
                                EntityBaseHelper.JsEscape(template.ToHtmlString())));
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
