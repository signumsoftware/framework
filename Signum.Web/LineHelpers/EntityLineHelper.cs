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
using Signum.Web.Controllers;
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

            if (settings.LabelVisible)
                sb.AppendLine(EntityBaseHelper.WriteLabel(helper, prefix, settings));

            if (isIdentifiable || isLite)
            {
                sb.AppendLine(helper.HiddenEntityInfo(prefix, new RuntimeInfo<T>(value) { Ticks = ticks }, new StaticInfo(cleanStaticType) { IsReadOnly = settings.ReadOnly }));

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
                {
                    if (settings.Implementations != null && settings.Implementations.IsByAll)
                        throw new InvalidOperationException("Autocomplete is not possible with ImplementedByAll");

                    sb.AppendLine(helper.AutoCompleteExtender(TypeContext.Compose(prefix, EntityLineKeys.DDL),
                                     TypeContext.Compose(prefix, EntityBaseKeys.ToStr),
                                     cleanStaticType.Name,
                                     ImplementationsModelBinder.Render(settings.Implementations),
                                     TypeContext.Compose(prefix, TypeContext.Id),
                                     "Signum/Autocomplete", 1, 5, 500, settings.OnChangedTotal.HasText() ? settings.OnChangedTotal : "''"));
                }
            }
            else
            {
                sb.AppendLine(helper.HiddenEntityInfo(prefix, new EmbeddedRuntimeInfo<T>(value, false) { Ticks = ticks }, new StaticInfo(cleanStaticType) { IsReadOnly = settings.ReadOnly }));

                typeContext.Value = (T)(object)Constructor.ConstructStrict(cleanRuntimeType ?? cleanStaticType);
                sb.AppendLine("<script type=\"text/javascript\">var {0} = \"{1}\"</script>".Formato(
                        TypeContext.Compose(prefix, EntityBaseKeys.Template),
                        EntityBaseHelper.JsEscape(EntityBaseHelper.RenderPopupContent(helper, prefix, typeContext, settings, cleanRuntimeType, cleanStaticType, typeof(Lite).IsAssignableFrom(typeof(T))))));
                typeContext.Value = value;

                if (value != null)
                    sb.AppendLine(EntityBaseHelper.RenderPopupInEntityDiv(helper, prefix, typeContext, settings, cleanRuntimeType, cleanStaticType, isLite));

                sb.AppendLine(helper.Span(TypeContext.Compose(prefix, EntityBaseKeys.ToStr), value.TryToString(), "valueLine"));
            }

            string id = (isIdentifiable) ? ((IIdentifiable)(object)value).TryCS(i => i.IdOrNull).TryToString() :
                (isLite) ? ((Lite)(object)value).TryCS(i => i.IdOrNull).TryToString() : 
                "";
            
            if (settings.Navigate && id.HasText())
            {
                sb.AppendLine(
                    helper.Href(TypeContext.Compose(prefix, EntityBaseKeys.ToStrLink),
                        value.ToString(), Navigator.ViewRoute(cleanRuntimeType, int.Parse(id)), Resources.View, "valueLine",
                        new Dictionary<string, object> { { "style", "display:" + ((value == null) ? "none" : "block") } }));
            }
            else if (isIdentifiable || isLite)
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
            Common.FireCommonTasks(el, context);

            if (settingsModifier != null)
                settingsModifier(el);

            using (el)
                helper.ViewContext.HttpContext.Response.Write(
                    helper.InternalEntityLine(context, el));
        }
    }
}
