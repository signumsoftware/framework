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
#endregion

namespace Signum.Web
{
    public static class EntityBaseHelper
    { 
        public static string WriteLabel(HtmlHelper helper, string prefix, BaseLine settings)
        {
            return StyleContext.Current.LabelVisible ?
                helper.Label(prefix + "lbl", settings.LabelText ?? "", TypeContext.Compose(prefix, EntityBaseKeys.ToStr), TypeContext.CssLineLabel) :
                "";
        }

        public static string WriteLabel(HtmlHelper helper, string prefix, BaseLine settings, string idLabelFor)
        {
            return StyleContext.Current.LabelVisible ?
                helper.Label(prefix + "lbl", settings.LabelText ?? "", idLabelFor, TypeContext.CssLineLabel) :
                "";
        }

        public static long? GetTicks(HtmlHelper helper, string prefix, BaseLine settings)
        {
            if ((StyleContext.Current.ShowTicks == null || StyleContext.Current.ShowTicks.Value) && 
                !StyleContext.Current.ReadOnly && 
                (helper.ViewData.ContainsKey(ViewDataKeys.Reactive) || settings.ReloadOnChange))
                return helper.GetChangeTicks(prefix) ?? 0;
            return null;
        }

        public static bool RequiresLoadAll<T>(HtmlHelper helper, bool isIdentifiable, bool isLite, T value, string prefix)
        {
            bool hasChanged = helper.GetChangeTicks(prefix) > 0;
            
            //To pre-load an entity in a Line, it has to have changed and also at least one of its properties
            Dictionary<string, long> ticks = (Dictionary<string, long>)helper.ViewData[ViewDataKeys.ChangeTicks];
            bool propertyHasChanged = ticks != null && ticks.Any(kvp => kvp.Key.StartsWith(prefix) && kvp.Key != prefix && kvp.Value > 0);
            
            return (helper.ViewData.ContainsKey(ViewDataKeys.LoadAll) && value != null && hasChanged && propertyHasChanged) ||
                    (isIdentifiable && value != null && (value as IIdentifiable).IsNew == true) ||
                    (isLite && value != null && (value as Lite).IdOrNull == null);
        }

        public static string RenderPopupInEntityDiv<T>(HtmlHelper helper, string prefix, TypeContext<T> typeContext, EntityBase settings, Type cleanRuntimeType, Type cleanStaticType, bool isLite)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<div id='{0}' name='{0}' style='display:none'>".Formato(TypeContext.Compose(prefix, EntityBaseKeys.Entity)));

            sb.AppendLine(RenderPopupContent<T>(helper, prefix, typeContext, settings, cleanRuntimeType, cleanStaticType, isLite));

            sb.AppendLine("</div>");

            return sb.ToString();
        }

        public static string RenderPopupContent<T>(HtmlHelper helper, string prefix, TypeContext<T> typeContext, EntityBase settings, Type cleanRuntimeType, Type cleanStaticType, bool isLite)
        {
            StringBuilder sb = new StringBuilder();

            EntitySettings es = Navigator.Manager.EntitySettings.TryGetC(cleanRuntimeType ?? cleanStaticType).ThrowIfNullC(Resources.TheresNotAViewForType0.Formato(cleanRuntimeType ?? cleanStaticType));

            TypeContext tc = typeContext;
            if (isLite)
                tc = typeContext.ExtractLite();

            ViewDataDictionary vdd = new ViewDataDictionary(tc)
            { 
                { ViewDataKeys.MainControlUrl, settings.PartialViewName ?? es.PartialViewName((ModifiableEntity)tc.UntypedValue)},
                { ViewDataKeys.PopupPrefix, helper.ParentPrefix() }
            };
            helper.PropagateSFKeys(vdd);
            if (settings.ReloadOnChange)
                vdd[ViewDataKeys.Reactive] = true;

            //using (var sc = StyleContext.RegisterCleanStyleContext(true))
            sb.AppendLine(helper.RenderPartialToString(Navigator.Manager.PopupControlUrl, vdd));

            return sb.ToString();
        }

        public static string RenderContent<T>(HtmlHelper helper, string prefix, TypeContext<T> typeContext, EntityBase settings, Type cleanRuntimeType, Type cleanStaticType, bool isLite)
        {
            StringBuilder sb = new StringBuilder();

            EntitySettings es = Navigator.Manager.EntitySettings.TryGetC(cleanRuntimeType ?? cleanStaticType).ThrowIfNullC(Resources.TheresNotAViewForType0.Formato(cleanRuntimeType ?? cleanStaticType));

            TypeContext tc = typeContext;
            if (isLite)
                tc = typeContext.ExtractLite();

            ViewDataDictionary vdd = new ViewDataDictionary(tc)
            { 
                { ViewDataKeys.MainControlUrl, helper.ParentPrefix() },
                { ViewDataKeys.PopupPrefix, helper.ParentPrefix() }
            };
            helper.PropagateSFKeys(vdd);
            if (settings.ReloadOnChange)
                vdd[ViewDataKeys.Reactive] = true;

            sb.AppendLine(helper.RenderPartialToString(settings.PartialViewName ?? es.PartialViewName((ModifiableEntity)tc.UntypedValue), vdd));

            return sb.ToString();
        }

        public static string JsEscape(string input)
        {
            return input.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("/", "\\/").Replace("\r\n", "").Replace("\n", "");
        }

        public static string WriteImplementations(HtmlHelper helper, EntityBase settings, string prefix)
        {
            if (settings.Implementations == null)
                return "";

            string implementations = ImplementationsModelBinder.Render(settings.Implementations);

            return helper.Hidden(TypeContext.Compose(prefix, EntityBaseKeys.Implementations), implementations);
        }

        public static string WriteViewButton<T>(HtmlHelper helper, EntityBase settings, T value)
        {
            if (!settings.View)
                return "";

            return helper.Button(TypeContext.Compose(settings.Prefix, "btnView"),
                  "->",
                  settings.GetViewing(),
                  "lineButton go",
                  (value == null) ? new Dictionary<string, object>() { { "style", "display:none" } } : new Dictionary<string, object>());
        }

        public static string WriteCreateButton<T>(HtmlHelper helper, EntityBase settings, T value)
        {
            if (!settings.Create && settings.Implementations == null)
                return "";

            return helper.Button(TypeContext.Compose(settings.Prefix, "btnCreate"),
                  "+",
                  settings.GetCreating(),
                  "lineButton create",
                  (value == null) ? new Dictionary<string, object>() : new Dictionary<string, object>() { { "style", "display:none" } });
        }

        public static string WriteFindButton<T>(HtmlHelper helper, EntityBase settings, T value, bool isIdentifiable, bool isLite)
        {
            if ((!isIdentifiable && !isLite) || (!settings.Find && settings.Implementations == null))
                return "";

            return helper.Button(TypeContext.Compose(settings.Prefix, "btnFind"),
                 "O",
                 settings.GetFinding(),
                 "lineButton find",
                 (value == null) ? new Dictionary<string, object>() : new Dictionary<string, object>() { { "style", "display:none" } });
        }

        public static string WriteRemoveButton<T>(HtmlHelper helper, EntityBase settings, T value)
        {
            if (!settings.Remove && settings.Implementations == null)
                return "";

            return helper.Button(TypeContext.Compose(settings.Prefix, "btnRemove"),
                  "x",
                  settings.GetRemoving(),
                  "lineButton remove",
                  (value == null) ? new Dictionary<string, object>() { { "style", "display:none" } } : new Dictionary<string, object>());        
        }

        public static string WriteBreakLine()
        {
            return StyleContext.Current.BreakLine ? "<div class='clearall'></div>" : "";
        }
    }
}
