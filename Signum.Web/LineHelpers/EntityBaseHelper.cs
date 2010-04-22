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
#endregion

namespace Signum.Web
{
    public static class EntityBaseHelper
    { 
        public static string BaseLineLabel(HtmlHelper helper, BaseLine baseLine)
        {
            return BaseLineLabel(helper, baseLine, baseLine.Compose(EntityBaseKeys.ToStr));
        }

        public static string BaseLineLabel(HtmlHelper helper, BaseLine baseLine, string idLabelFor)
        {
            return baseLine.LabelVisible ?
                           helper.Label(baseLine.Compose("lbl"), baseLine.LabelText ?? "", idLabelFor, TypeContext.CssLineLabel) :
                           "";
        }

        public static bool RequiresLoadAll(HtmlHelper helper, EntityBase eb)
        {
            bool hasChanged = helper.GetChangeTicks(eb.ControlID) > 0;
            
            //To pre-load an entity in a Line, it has to have changed and also at least one of its properties
            Dictionary<string, long> ticks = (Dictionary<string, long>)helper.ViewData[ViewDataKeys.ChangeTicks];
            bool propertyHasChanged = ticks != null && ticks.Any(kvp => kvp.Value > 0 && kvp.Key.StartsWith(eb.ControlID) && kvp.Key != eb.ControlID);
            
            return (eb.IsNew == true) || 
                   (eb.UntypedValue != null && hasChanged && propertyHasChanged);
        }

        public static string RenderTypeContext(HtmlHelper helper, TypeContext typeContext, RenderMode mode, string partialViewName, bool reloadOnChange)
        {
            Type cleanRuntimeType = (typeContext.UntypedValue as Lite).TryCC(l => l.RuntimeType) ?? typeContext.UntypedValue.GetType();

            EntitySettings es = Navigator.Manager.EntitySettings.TryGetC(cleanRuntimeType).ThrowIfNullC(Resources.TheresNotAViewForType0.Formato(cleanRuntimeType));

            TypeContext tc = TypeContextUtilities.CleanTypeContext((TypeContext)typeContext);

            ViewDataDictionary vdd = new ViewDataDictionary(tc);

            helper.PropagateSFKeys(vdd);
            if (reloadOnChange)
                vdd[ViewDataKeys.Reactive] = true;
            
            if (string.IsNullOrEmpty(partialViewName))
                partialViewName = es.OnPartialViewName((ModifiableEntity)tc.UntypedValue);

            switch (mode)
            {
                case RenderMode.Content:
                    return helper.RenderPartialToString(partialViewName, vdd);
                case RenderMode.Popup:
                    vdd.Add(ViewDataKeys.PartialViewName, partialViewName);
                    return helper.RenderPartialToString(Navigator.Manager.PopupControlUrl, vdd);
                case RenderMode.PopupInDiv:
                    vdd.Add(ViewDataKeys.PartialViewName, partialViewName);
                    return helper.Div(typeContext.Compose(EntityBaseKeys.Entity),
                        helper.RenderPartialToString(Navigator.Manager.PopupControlUrl, vdd),
                        "",
                        new Dictionary<string, object> { { "style", "display:none" } });
                case RenderMode.ContentInVisibleDiv:
                case RenderMode.ContentInInvisibleDiv:
                    return helper.Div(typeContext.Compose(EntityBaseKeys.Entity),
                        helper.RenderPartialToString(partialViewName, vdd),
                        "",
                        (mode == RenderMode.ContentInInvisibleDiv) ? new Dictionary<string, object> { { "style", "display:none" } } : null);
                default:
                    throw new InvalidOperationException();
            }
        }

        public static string JsEscape(string input)
        {
            return input.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("/", "\\/").Replace("\r\n", "").Replace("\n", "");
        }

        public static string WriteImplementations(HtmlHelper helper, EntityBase entityBase)
        {
            if (entityBase.Implementations == null)
                return "";

            string implementations = ImplementationsModelBinder.Render(entityBase.Implementations);

            return helper.Hidden(entityBase.Compose(EntityBaseKeys.Implementations), implementations, new { disabled = "disabled"});
        }

        public static string WriteViewButton(HtmlHelper helper, EntityBase entityBase)
        {
            if (!entityBase.View)
                return "";

            return helper.Button(entityBase.Compose("btnView"),
                  "->",
                  entityBase.GetViewing(),
                  "lineButton go",
                  (entityBase.UntypedValue == null) ? new Dictionary<string, object>() { { "style", "display:none" } } : new Dictionary<string, object>());
        }

        public static string WriteCreateButton(HtmlHelper helper, EntityBase entityBase)
        {
            if (!entityBase.Create && entityBase.Implementations == null)
                return "";

            return helper.Button(entityBase.Compose("btnCreate"),
                  "+",
                  entityBase.GetCreating(),
                  "lineButton create",
                  (entityBase.UntypedValue == null) ? new Dictionary<string, object>() : new Dictionary<string, object>() { { "style", "display:none" } });
        }

        public static string WriteFindButton(HtmlHelper helper, EntityBase entityBase)
        {
            if ((!entityBase.Type.IsIIdentifiable() && !entityBase.Type.IsLite()) || (!entityBase.Find && entityBase.Implementations == null))
                return "";

            return helper.Button(entityBase.Compose("btnFind"),
                 "O",
                 entityBase.GetFinding(),
                 "lineButton find",
                 (entityBase.UntypedValue == null) ? new Dictionary<string, object>() : new Dictionary<string, object>() { { "style", "display:none" } });
        }

        public static string WriteRemoveButton(HtmlHelper helper, EntityBase entityBase)
        {
            if (!entityBase.Remove && entityBase.Implementations == null)
                return "";

            return helper.Button(entityBase.Compose("btnRemove"),
                  "x",
                  entityBase.GetRemoving(),
                  "lineButton remove",
                  (entityBase.UntypedValue == null) ? new Dictionary<string, object>() { { "style", "display:none" } } : new Dictionary<string, object>());        
        }

        public static string WriteBreakLine(HtmlHelper helper, EntityBase entityBase)
        {
            return entityBase.BreakLine ? helper.Div("", "", "clearall") : "";
        }

        internal static string EmbeddedTemplate(EntityBase entityBase, string template)
        {
            return "<script type=\"text/javascript\">var {0} = \"{1}\"</script>".Formato(
                                entityBase.Compose(EntityBaseKeys.Template),
                                EntityBaseHelper.JsEscape(template));
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
