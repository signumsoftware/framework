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
using Signum.Engine;
using System.Configuration;
using Signum.Web.Properties;
#endregion

namespace Signum.Web
{
    public static class EntityComboHelper
    {
        internal static void InternalEntityCombo<T>(this HtmlHelper helper, TypeContext<T> typeContext, EntityCombo settings)
        {
            if (!settings.Visible)
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

            sb.AppendLine(EntityBaseHelper.WriteLabel(helper, prefix, settings, TypeContext.Compose(prefix, EntityComboKeys.Combo)));

            if (isIdentifiable || isLite)
            {
                sb.AppendLine(helper.HiddenSFInfo(prefix, new EntityInfo<T>(cleanStaticType, value) { Ticks = ticks }));

                if (EntityBaseHelper.RequiresLoadAll(helper, isIdentifiable, isLite, value))
                    sb.AppendLine(EntityBaseHelper.RenderPopupInEntityDiv(helper, prefix, typeContext, settings, cleanRuntimeType, cleanStaticType, isLite));
                else if (value != null)
                    sb.AppendLine(helper.Div(TypeContext.Compose(prefix, EntityBaseKeys.Entity), "", "", new Dictionary<string, object> { { "style", "display:none" } }));

                if (settings.Implementations != null)
                    throw new ApplicationException("Types with Implementations are not allowed for EntityCombo yet");

                if (StyleContext.Current.ReadOnly)
                    sb.AppendLine(helper.Span(prefix, (value != null) ? value.ToString() : "", "valueLine"));
                else
                {
                    List<SelectListItem> items = new List<SelectListItem>();
                    items.Add(new SelectListItem() { Text = "-", Value = "" });
                    if (settings.Preload)
                    {
                        if (settings.Data != null)
                        {
                            items.AddRange(
                                settings.Data.Select(lite => new SelectListItem()
                                    {
                                        Text = lite.ToString(),
                                        Value = lite.Id.ToString(),
                                        Selected = (value != null) && (lite.Id == ((IIdentifiable)(object)value).TryCS(i => i.IdOrNull))
                                    })
                                );
                        }
                        else
                        {
                            items.AddRange(
                                Database.RetrieveAllLite(cleanStaticType)
                                    .Select(lite => new SelectListItem()
                                    {
                                        Text = lite.ToString(),
                                        Value = lite.Id.ToString(),
                                        Selected = (value != null) && (lite.Id == ((IIdentifiable)(object)value).TryCS(i => i.IdOrNull))
                                    })
                                );
                        }
                    }

                    settings.ComboHtmlProperties.Add("class","valueLine");

                    if (settings.ComboHtmlProperties.ContainsKey("onchange"))
                        throw new ApplicationException("EntityCombo cannot have onchange html property, use onEntityChanged instead");

                    settings.ComboHtmlProperties.Add("onchange", "EComboOnChanged({0});".Formato(settings.ToJS()));

                    sb.AppendLine(helper.DropDownList(
                        TypeContext.Compose(prefix, EntityComboKeys.Combo),
                        items,
                        settings.ComboHtmlProperties));
                }
            }

            sb.AppendLine(EntityBaseHelper.WriteViewButton(helper, settings, value));

            sb.AppendLine(EntityBaseHelper.WriteCreateButton(helper, settings, value));

            sb.AppendLine(EntityBaseHelper.WriteBreakLine());

            helper.ViewContext.HttpContext.Response.Write(sb.ToString());
        }

        public static void EntityCombo<T,S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property) 
        {
            helper.EntityCombo<T, S>(tc, property, null);
        }

        public static void EntityCombo<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<EntityCombo> settingsModifier)
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

            EntityCombo ec = new EntityCombo(helper.GlobalName(context.Name));
            Navigator.ConfigureEntityBase(ec, runtimeType, false);
            Common.FireCommonTasks(ec, typeof(T), context);

            if (settingsModifier != null)
                settingsModifier(ec);

            using (ec)
                helper.InternalEntityCombo(context, ec);
        }
    }
}
