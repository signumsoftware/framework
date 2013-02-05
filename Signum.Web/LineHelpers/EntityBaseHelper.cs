#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using Signum.Utilities;
using Signum.Entities;
using Signum.Web.Properties;
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

        public static bool RequiresLoadAll(HtmlHelper helper, EntityBase eb)
        {
            return eb.IsNew == true;
        }

        public static MvcHtmlString RenderTypeContext(HtmlHelper helper, TypeContext typeContext, RenderMode mode, EntityBase line)
        {
            Type cleanEntityType = (typeContext.UntypedValue as Lite<IIdentifiable>).TryCC(l => l.EntityType) ?? typeContext.UntypedValue.GetType();

            EntitySettings es = Navigator.Manager.EntitySettings.TryGetC(cleanEntityType)
                .ThrowIfNullC("There's no EntitySettings registered for type {0}".Formato(cleanEntityType));

            TypeContext tc = TypeContextUtilities.CleanTypeContext((TypeContext)typeContext);

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
            
            string partialViewName = line.PartialViewName;
            if (string.IsNullOrEmpty(partialViewName))
                partialViewName = es.OnPartialViewName((ModifiableEntity)tc.UntypedValue);

            switch (mode)
            {
                case RenderMode.Content:
                    return helper.Partial(partialViewName, vdd);
                case RenderMode.Popup:
                    vdd[ViewDataKeys.PartialViewName] = partialViewName;
                    vdd[ViewDataKeys.OkVisible] = !line.ReadOnly;
                    vdd[ViewDataKeys.ViewButtons] = ViewButtons.Ok;
                    vdd[ViewDataKeys.ShowOperations] = true;
                    vdd[ViewDataKeys.SaveProtected] = OperationLogic.IsSaveProtected(tc.UntypedValue.GetType());
                    return helper.Partial(Navigator.Manager.PopupControlView, vdd);
                case RenderMode.PopupInDiv:
                    vdd[ViewDataKeys.PartialViewName] = partialViewName;
                    vdd[ViewDataKeys.OkVisible] = !line.ReadOnly;
                    vdd[ViewDataKeys.ViewButtons] = ViewButtons.Ok;
                    vdd[ViewDataKeys.ShowOperations] = true;
                    vdd[ViewDataKeys.SaveProtected] = OperationLogic.IsSaveProtected(tc.UntypedValue.GetType());
                    return helper.Div(typeContext.Compose(EntityBaseKeys.Entity),
                        helper.Partial(Navigator.Manager.PopupControlView, vdd),
                        "",
                        new Dictionary<string, object> { { "style", "display:none" } });
                case RenderMode.ContentInVisibleDiv:
                case RenderMode.ContentInInvisibleDiv:
                    return helper.Div(typeContext.Compose(EntityBaseKeys.Entity),
                        helper.Partial(partialViewName, vdd), "",
                        (mode == RenderMode.ContentInInvisibleDiv) ? new Dictionary<string, object> { { "style", "display:none" } } : null);
                default:
                    throw new InvalidOperationException();
            }
        }

        public static string JsEscape(string input)
        {
            return input.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("/", "\\/").Replace("\r\n", "").Replace("\n", "");
        }

        public static MvcHtmlString ViewButton(HtmlHelper helper, EntityBase entityBase)
        {
            if (!entityBase.View)
                return MvcHtmlString.Empty;

            var htmlAttr = new Dictionary<string, object>
            {
                { "onclick", new MvcHtmlString(entityBase.GetViewing()) },
                { "data-icon",  "ui-icon-circle-arrow-e" },
                { "data-text", false}
            };

            if (entityBase.UntypedValue == null)
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
                { "onclick", new MvcHtmlString(entityBase.GetViewing()) },
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

        public static MvcHtmlString CreateButton(HtmlHelper helper, EntityBase entityBase)
        {
            if (!entityBase.Create)
                return MvcHtmlString.Empty;

            Type type = entityBase.Type.CleanType();

            var htmlAttr = new Dictionary<string, object>
            {
                { "onclick", entityBase.GetCreating() },
                { "data-icon", "ui-icon-circle-plus" },
                { "data-text", false}
            };

            if (entityBase.UntypedValue != null)
                htmlAttr.Add("style", "display:none");

            return helper.Href(entityBase.Compose("btnCreate"),
                  EntityControlMessage.Create.NiceToString(),
                  "",
                  EntityControlMessage.Create.NiceToString(),
                  "sf-line-button sf-create",
                  htmlAttr);
        }

        public static MvcHtmlString FindButton(HtmlHelper helper, EntityBase entityBase)
        {
            if (!entityBase.Find)
                return MvcHtmlString.Empty;

            var htmlAttr = new Dictionary<string, object>
            {
                { "onclick", entityBase.GetFinding() },
                { "data-icon", "ui-icon-circle-zoomin" },
                { "data-text", false}
            };

            if (entityBase.UntypedValue != null)
                htmlAttr.Add("style", "display:none");

            return helper.Href(entityBase.Compose("btnFind"),
                  EntityControlMessage.Find.NiceToString(),
                  "",
                  EntityControlMessage.Find.NiceToString(),
                  "sf-line-button sf-find",
                  htmlAttr);
        }

        public static MvcHtmlString RemoveButton(HtmlHelper helper, EntityBase entityBase)
        {
            if (!entityBase.Remove)
                return MvcHtmlString.Empty;

            var htmlAttr = new Dictionary<string, object>
            {
                { "onclick", entityBase.GetRemoving() },
                { "data-icon", "ui-icon-circle-close" },
                { "data-text", false}
            };

            if (entityBase.UntypedValue == null)
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
                cleanType.IsEmbeddedEntity() ? Navigator.IsViewable(cleanType) :
                eb.Implementations.Value.IsByAll ? true :
                eb.Implementations.Value.Types.Any(t => Navigator.IsViewable(t));

            eb.Navigate &=
              cleanType.IsEmbeddedEntity() ? Navigator.IsNavigable(cleanType, isSearchEntity: false) :
              eb.Implementations.Value.IsByAll ? true :
              eb.Implementations.Value.Types.Any(t => Navigator.IsNavigable(t, isSearchEntity: false));

            eb.Find &=
                cleanType.IsEmbeddedEntity() ? false :
                eb.Implementations.Value.IsByAll ? false :
                eb.Implementations.Value.Types.Any(t => Navigator.IsFindable(t));
        }
    }

    public enum RenderMode
    {
        Popup,
        PopupInDiv,
        Content,
        ContentInVisibleDiv,
        ContentInInvisibleDiv
    }   
}
