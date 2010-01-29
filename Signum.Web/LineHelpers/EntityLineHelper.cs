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
    public static class EntityLineHelper
    {
        // This line returns string instead of void as all the other internalLines because it's used when constructing filters dinamically
        internal static string InternalEntityLine<T>(this HtmlHelper helper, TypeContext<T> typeContext, EntityLine settings)
        {
            if (!settings.Visible || settings.HideIfNull && typeContext.Value == null)
                return null;

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

            sb.AppendLine(EntityBaseHelper.WriteLabel(helper, prefix, settings));

            if (isIdentifiable || isLite)
            {
                sb.AppendLine(helper.HiddenSFInfo(prefix, new EntityInfo<T>(value) { Ticks = ticks }));

                sb.AppendLine(EntityBaseHelper.WriteImplementations(helper, settings, prefix));

                if (EntityBaseHelper.RequiresLoadAll(helper, isIdentifiable, isLite, value, prefix))
                    sb.AppendLine(EntityBaseHelper.RenderPopupInEntityDiv(helper, prefix, typeContext, settings, cleanRuntimeType, cleanStaticType, isLite));
                else if (value != null)
                    sb.AppendLine(helper.Div(TypeContext.Compose(prefix, EntityBaseKeys.Entity), "", "", new Dictionary<string, object> { { "style", "display:none" } }));
                
                sb.AppendLine(helper.TextBox(
                    TypeContext.Compose(prefix, EntityBaseKeys.ToStr), 
                    (isIdentifiable) 
                        ? ((IdentifiableEntity)(object)value).TryCC(i => i.ToStr) 
                        : ((Lite)(object)value).TryCC(i => i.ToStr), 
                    new Dictionary<string, object>() 
                    { 
                        { "class", "valueLine" }, 
                        { "autocomplete", "off" }, 
                        { "style", "display:" + ((value==null && !settings.ReadOnly) ? "block" : "none")}
                    }));

                if (settings.Autocomplete && Navigator.NameToType.ContainsKey(cleanStaticType.Name))
                    sb.AppendLine(helper.AutoCompleteExtender(TypeContext.Compose(prefix, EntityLineKeys.DDL),
                                                      TypeContext.Compose(prefix, EntityBaseKeys.ToStr),
                                                      cleanStaticType.Name,
                                                      (settings.Implementations != null) ? settings.Implementations.ToString(t => t.Name, ",") : "",
                                                      TypeContext.Compose(prefix, TypeContext.Id),
                                                      "Signum/Autocomplete", 1, 5, 500, settings.OnChangedTotal.HasText() ? settings.OnChangedTotal : "''"));
            }
            else
            {
                sb.AppendLine(helper.HiddenSFInfo(prefix, new EmbeddedEntityInfo<T>(value, false) { Ticks = ticks }));

                sb.AppendLine(EntityBaseHelper.RenderPopupInEntityDiv(helper, prefix, typeContext, settings, cleanRuntimeType, cleanStaticType, isLite));

                sb.AppendLine(helper.Span(TypeContext.Compose(prefix, EntityBaseKeys.ToStr), value.ToString(), "valueLine", new Dictionary<string, object> { { "style", "display:" + ((value == null) ? "block" : "none") } }));
            }

            string id = (isIdentifiable) ? ((IIdentifiable)(object)value).TryCS(i => i.IdOrNull).TryToString() :
                (isLite) ? ((Lite)(object)value).TryCS(i => i.IdOrNull).TryToString() : 
                "";
            
            if (settings.Navigate)
            {
                string viewingUrl = "";
                string linkText = "";
                if (id.HasText())
                {
                    viewingUrl = Navigator.ViewRoute(cleanRuntimeType, int.Parse(id));
                    linkText = value.ToString();
                }
                sb.AppendLine(
                    helper.Href(TypeContext.Compose(prefix, EntityBaseKeys.ToStrLink),
                        linkText, viewingUrl, "View", "valueLine",
                        new Dictionary<string, object> { { "style", "display:" + ((value == null) ? "none" : "block") } }));
            }
            else
            {
                sb.AppendLine(
                    helper.Span(TypeContext.Compose(prefix, EntityBaseKeys.ToStrLink),
                        (value != null) ? value.ToString() : "&nbsp;",
                        "valueLine",
                        new Dictionary<string, object> { { "style", "display:" + ((value == null) ? "none" : "block") } }));
            }

            sb.AppendLine(EntityBaseHelper.WriteViewButton(helper, settings, value));
            sb.AppendLine(EntityBaseHelper.WriteCreateButton(helper, settings, value));
            sb.AppendLine(EntityBaseHelper.WriteFindButton(helper, settings, value, isIdentifiable, isLite));
            sb.AppendLine(EntityBaseHelper.WriteRemoveButton(helper, settings, value));

            sb.AppendLine(EntityBaseHelper.WriteBreakLine());

            return sb.ToString();
        }

        public static void EntityLine<T,S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property) 
        {
            helper.EntityLine<T, S>(tc, property, null);
        }

        public static void EntityLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<EntityLine> settingsModifier)
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
            
            EntityLine el = new EntityLine(helper.GlobalName(context.Name));
            Navigator.ConfigureEntityBase(el, runtimeType, false);
            Common.FireCommonTasks(el, typeof(T), context);

            if (settingsModifier != null)
                settingsModifier(el);

            using (el)
                helper.ViewContext.HttpContext.Response.Write(
                    helper.InternalEntityLine(context, el));
        }
    }
}
