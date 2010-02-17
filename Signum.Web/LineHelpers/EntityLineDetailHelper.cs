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
using Signum.Web.Properties;
#endregion

namespace Signum.Web
{
    public static class EntityLineDetailHelper
    {
        internal static void InternalEntityLineDetail<T>(this HtmlHelper helper, TypeContext<T> typeContext, EntityLineDetail settings)
        {
            if (!settings.Visible || settings.HideIfNull && typeContext.Value == null)
                return;

            string prefix = helper.GlobalName(typeContext.Name);
            T value = typeContext.Value;
            Type cleanStaticType = Reflector.ExtractLite(typeof(T)) ?? typeof(T); //typeContext.ContextType;
            bool isIdentifiable = typeof(IIdentifiable).IsAssignableFrom(typeof(T));
            bool isLite = typeof(Lite).IsAssignableFrom(typeof(T));
            
            Type cleanRuntimeType = null;
            if (value != null)
                cleanRuntimeType = typeof(Lite).IsAssignableFrom(value.GetType()) ? (value as Lite).RuntimeType : value.GetType();

            long? ticks = EntityBaseHelper.GetTicks(helper, prefix, settings);

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<div class='EntityLineDetail'>");

            sb.AppendLine(EntityBaseHelper.WriteLabel(helper, prefix, settings));

            if (isIdentifiable || isLite)
            {
                sb.AppendLine(helper.HiddenEntityInfo(prefix, new RuntimeInfo<T>(value) { Ticks = ticks }, new StaticInfo(cleanStaticType) { IsReadOnly = settings.ReadOnly }));

                sb.AppendLine(EntityBaseHelper.WriteImplementations(helper, settings, prefix));
            }
            else
            {
                sb.AppendLine(helper.HiddenEntityInfo(prefix, new EmbeddedRuntimeInfo<T>(value, false) { Ticks = ticks }, new StaticInfo(cleanStaticType) { IsReadOnly = settings.ReadOnly }));
                
                typeContext.Value = (T)(object)Constructor.ConstructStrict(cleanRuntimeType ?? cleanStaticType);
                sb.AppendLine("<script type=\"text/javascript\">var {0} = \"{1}\";</script>".Formato(
                        TypeContext.Compose(prefix, EntityBaseKeys.Template),
                        EntityBaseHelper.JsEscape(EntityBaseHelper.RenderContent(helper, prefix, typeContext, settings, cleanRuntimeType, cleanStaticType, typeof(Lite).IsAssignableFrom(typeof(T))))));
                typeContext.Value = value;
            }

            sb.AppendLine(EntityBaseHelper.WriteCreateButton(helper, settings, value));
            sb.AppendLine(EntityBaseHelper.WriteFindButton(helper, settings, value, isIdentifiable, isLite));
            sb.AppendLine(EntityBaseHelper.WriteRemoveButton(helper, settings, value));

            sb.AppendLine(EntityBaseHelper.WriteBreakLine());

            string controlHtml = null;
            if (value != null)
                controlHtml = EntityBaseHelper.RenderContent<T>(helper, prefix, typeContext, settings, cleanRuntimeType, cleanStaticType, isLite);
            
            if (settings.DetailDiv == settings.DefaultDetailDiv)
                sb.AppendLine("<div id='{0}' name='{0}'>{1}</div>".Formato(settings.DetailDiv, controlHtml ?? ""));
            else if (controlHtml != null)
                sb.AppendLine("<script type=\"text/javascript\">\n" +
                        "$(document).ready(function() {\n" +
                        "$('#" + settings.DetailDiv + "').html(" + controlHtml + ");\n" +
                        "});\n" +
                        "</script>");

            sb.AppendLine("</div>"); //Closing tag of <div class='EntityLineDetail'>

            helper.ViewContext.HttpContext.Response.Write(sb.ToString());
        }

        public static void EntityLineDetail<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property) 
        {
            helper.EntityLineDetail<T, S>(tc, property, null);
        }

        public static void EntityLineDetail<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<EntityLineDetail> settingsModifier)
        {
            TypeContext<S> context = Common.WalkExpression(tc, property);

            Type runtimeType = typeof(S);
            if (context.Value != null)
            {
                if (typeof(Lite).IsAssignableFrom(context.Value.GetType()))
                    runtimeType = (context.Value as Lite).RuntimeType;
                else
                    runtimeType = context.Value.GetType();
            }
            else
                runtimeType = Reflector.ExtractLite(runtimeType) ?? runtimeType;

            EntityLineDetail el = new EntityLineDetail(helper.GlobalName(context.Name));
            Navigator.ConfigureEntityBase(el, runtimeType, false);
            Common.FireCommonTasks(el, context);

            if (settingsModifier != null)
                settingsModifier(el);

            using (el)
                helper.InternalEntityLineDetail(context, el);
        }
    }
}
