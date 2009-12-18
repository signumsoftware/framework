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
#endregion

namespace Signum.Web
{
    public static class ListBaseHelper
    {
        public static string RenderItemPopupInEntityDiv<T>(HtmlHelper helper, string indexedPrefix, TypeContext<MList<T>> listTypeContext, T itemValue, int index, EntityListBase settings, Type cleanRuntimeType, Type cleanStaticType, bool isLite)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<div id='{0}' name='{0}' style='display:none'>".Formato(TypeContext.Compose(indexedPrefix, EntityBaseKeys.Entity)));

            EntitySettings es = Navigator.Manager.EntitySettings.TryGetC(cleanRuntimeType ?? cleanStaticType).ThrowIfNullC(Resources.TheresNotAViewForType0.Formato(cleanRuntimeType ?? cleanStaticType));
            
            TypeContext tc;
            if (isLite)
                tc = (TypeContext)Activator.CreateInstance(typeof(TypeElementContext<>).MakeGenericType(cleanRuntimeType), new object[] { Database.Retrieve((Lite)(object)itemValue), listTypeContext, index });
            else
                tc = new TypeElementContext<T>(itemValue, listTypeContext, index);
            
            ViewDataDictionary vdd = new ViewDataDictionary(tc)
            { 
                { ViewDataKeys.MainControlUrl, settings.PartialViewName ?? es.PartialViewName},
                //{ ViewDataKeys.PopupPrefix, indexedPrefix} //Now prefix is in TypeElementContext 
            };
            helper.PropagateSFKeys(vdd);
            if (settings.ReloadOnChange || settings.ReloadFunction.HasText())
                vdd[ViewDataKeys.Reactive] = true;

            using (var sc = StyleContext.RegisterCleanStyleContext(true))
                sb.Append(helper.RenderPartialToString(Navigator.Manager.PopupControlUrl, vdd));
            
            sb.AppendLine("</div>");

            return sb.ToString();
        }

        public static string RenderItemContentInEntityDiv<T>(HtmlHelper helper, string indexedPrefix, TypeContext<MList<T>> listTypeContext, T itemValue, int index, EntityListBase settings, Type cleanRuntimeType, Type cleanStaticType, bool isLite, bool visibleDiv)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<div id='{0}' name='{0}'{1}>".Formato(
                TypeContext.Compose(indexedPrefix, EntityBaseKeys.Entity),
                visibleDiv ? "" : " style='display:none'"));

            EntitySettings es = Navigator.Manager.EntitySettings.TryGetC(cleanRuntimeType ?? cleanStaticType).ThrowIfNullC(Resources.TheresNotAViewForType0.Formato(cleanRuntimeType ?? cleanStaticType));

            TypeContext tc;
            if (isLite)
                tc = (TypeContext)Activator.CreateInstance(typeof(TypeElementContext<>).MakeGenericType(cleanRuntimeType), new object[] { Database.Retrieve((Lite)(object)itemValue), listTypeContext, index });
            else
                tc = new TypeElementContext<T>(itemValue, listTypeContext, index);
            
            ViewDataDictionary vdd = new ViewDataDictionary(tc);
            helper.PropagateSFKeys(vdd);
            vdd[ViewDataKeys.PopupPrefix] = helper.ParentPrefix();
            if (settings.ReloadOnChange || settings.ReloadFunction.HasText())
                vdd[ViewDataKeys.Reactive] = true;

            sb.AppendLine(
                helper.RenderPartialToString(settings.PartialViewName ?? es.PartialViewName, vdd));
            sb.AppendLine("</div>");

            return sb.ToString();
        }

        public static string WriteCreateButton(HtmlHelper helper, EntityListBase settings, Dictionary<string, object> htmlProperties)
        {
            if (!settings.Create && settings.Implementations == null)
                return "";

            return helper.Button(TypeContext.Compose(settings.Prefix, "btnCreate"),
                  "+",
                  settings.GetCreating(),
                  "lineButton create",
                  htmlProperties ?? new Dictionary<string, object>());
        }

        public static string WriteFindButton(HtmlHelper helper, EntityListBase settings, Type elementsCleanStaticType)
        {
            if ((!settings.Find && settings.Implementations == null) || typeof(EmbeddedEntity).IsAssignableFrom(elementsCleanStaticType))
                return "";
            
            return helper.Button(TypeContext.Compose(settings.Prefix, "btnFind"),
                  "O",
                  settings.GetFinding(),
                  "lineButton find",
                  new Dictionary<string, object>());
        }

        public static string WriteRemoveButton<T>(HtmlHelper helper, EntityListBase settings, MList<T> value)
        {
            if (!settings.Remove && settings.Implementations == null)
                return "";

            return helper.Button(TypeContext.Compose(settings.Prefix, "btnRemove"),
                  "O",
                  settings.GetRemoving(),
                  "lineButton remove",
                  (value == null || value.Count == 0) ? new Dictionary<string, object>() { { "style", "display:none" } } : new Dictionary<string, object>());
        }
    }
}
