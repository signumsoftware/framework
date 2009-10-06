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

namespace Signum.Web
{
    public static class EntityComboKeys
    {
        public const string Combo = "sfCombo";
    }

    public class EntityCombo : EntityBase
    {
        public readonly Dictionary<string, object> ComboHtmlProperties = new Dictionary<string, object>(0);
        bool preload=true;
        public bool Preload
        {
            get { return preload; }
            set { preload=value; }
        }
        public EntityCombo()
        {
            Create = false;
            Remove = false;
            Find = false;
        }

        public override void SetReadOnly()
        {
            Find = false;
            Create = false;
            Remove = false;
            Implementations = null;
        }
    }

    public static class EntityComboHelper
    {
        internal static string InternalEntityCombo(this HtmlHelper helper, string idValueField, Type type, object value, EntityCombo settings)
        {
            if (!settings.Visible)
                return null;

            idValueField = helper.GlobalName(idValueField);
            string divASustituir = helper.GlobalName("divASustituir");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(helper.Hidden(TypeContext.Compose(idValueField, TypeContext.StaticType), (Reflector.ExtractLazy(type) ?? type).Name));

            if (StyleContext.Current.LabelVisible)
                sb.AppendLine(helper.Label(idValueField + "lbl", settings.LabelText ?? "", TypeContext.Compose(idValueField, EntityComboKeys.Combo), TypeContext.CssLineLabel));

            string runtimeType = "";
            if (value != null)
            {
                Type cleanRuntimeType = value.GetType();
                if (typeof(Lazy).IsAssignableFrom(value.GetType()))
                    cleanRuntimeType = (value as Lazy).RuntimeType;
                runtimeType = cleanRuntimeType.Name;
            }
            sb.AppendLine(helper.Hidden(TypeContext.Compose(idValueField, TypeContext.RuntimeType), runtimeType));
                
            bool isIdentifiable = typeof(IdentifiableEntity).IsAssignableFrom(type);
            bool isLazy = typeof(Lazy).IsAssignableFrom(type);
            if (isIdentifiable || isLazy)
            {
                sb.AppendLine(helper.Hidden(
                    TypeContext.Compose(idValueField, TypeContext.Id), 
                    (isIdentifiable) 
                       ? ((IIdentifiable)(object)value).TryCS(i => i.Id).TrySS(id => id)
                       : ((Lazy)(object)value).TryCS(i => i.Id).TrySS(id => id)));

                sb.AppendLine(helper.Div(TypeContext.Compose(idValueField, EntityBaseKeys.Entity), "", "", new Dictionary<string, object> { { "style", "display:none" } }));

                if (!StyleContext.Current.ReadOnly)
                {
                    List<SelectListItem> items = new List<SelectListItem>();
                    items.Add(new SelectListItem() { Text = "-", Value = "", Selected = true });
                    if (settings.Preload)
                    {
                        items.AddRange(
                            Database.RetrieveAllLazy(Reflector.ExtractLazy(type) ?? type)
                                .Select(lazy => new SelectListItem()
                                {
                                    Text = lazy.ToString(),
                                    Value = lazy.Id.ToString(),
                                    Selected = (value != null) && (lazy.Id == ((IIdentifiable)(object)value).TryCS(i => i.Id))
                                })
                            );
                    }

                    settings.ComboHtmlProperties.Add("class","valueLine");

                    if (settings.ComboHtmlProperties.ContainsKey("onchange"))
                        settings.ComboHtmlProperties["onchange"] = "EntityComboOnChange('{0}'); ".Formato(idValueField) + settings.ComboHtmlProperties["onchange"];
                    else
                        settings.ComboHtmlProperties.Add("onchange", "EntityComboOnChange('{0}');".Formato(idValueField));

                    sb.AppendLine(helper.DropDownList(
                        TypeContext.Compose(idValueField, EntityComboKeys.Combo),
                        items,
                        settings.ComboHtmlProperties));
                }
                else
                {
                    sb.AppendLine(helper.Span(idValueField, (value!=null) ? value.ToString() : "", "valueLine"));
                }
            }

            if (settings.View)
            {
                string viewingUrl = "javascript:OpenPopup('{0}','{1}','{2}',function(){{OnPopupComboOk('{3}','{2}');}},function(){{OnPopupComboCancel('{2}');}});".Formato("Signum.aspx/PopupView", divASustituir, idValueField, "Signum.aspx/ValidatePartial");
                sb.AppendLine(helper.Button(
                            TypeContext.Compose(idValueField, "btnView"),
                            "->",
                            viewingUrl,
                            "lineButton go",
                            new Dictionary<string, object> { { "style", "display:" + ((value == null) ? "none" : "inline") } }));
            }

            if (settings.Create)
            {
                string creatingUrl = "javascript:NewPopup('{0}','{1}','{2}',function(){{OnPopupComboOk('{3}','{2}');}},function(){{OnPopupComboCancel('{2}');}});".Formato("Signum.aspx/PopupView", divASustituir, idValueField, "Signum.aspx/ValidatePartial", (typeof(EmbeddedEntity).IsAssignableFrom(type)));
                sb.AppendLine(
                    helper.Button(TypeContext.Compose(idValueField, "btnCreate"),
                              "+",
                              creatingUrl,
                              "lineButton create",
                              (value == null) ? new Dictionary<string, object>() : new Dictionary<string, object>() { { "style", "display:none" } }));
            }

            sb.AppendLine("<script type=\"text/javascript\">var " + TypeContext.Compose(idValueField, EntityBaseKeys.EntityTemp) + " = '';</script>");
            
            if (StyleContext.Current.BreakLine)
                sb.AppendLine("<div class='clearall'></div>");

            return sb.ToString();
        }

        public static void EntityCombo<T,S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property) 
            where S : Modifiable 
        {
            TypeContext<S> context = Common.WalkExpression(tc, property);

            Type runtimeType = typeof(S);
            if (context.Value != null)
            {
                if (typeof(Lazy).IsAssignableFrom(context.Value.GetType()))
                    runtimeType = (context.Value as Lazy).RuntimeType;
                else
                    runtimeType = context.Value.GetType();
            }
            else
            {
                runtimeType = Reflector.ExtractLazy(runtimeType) ?? runtimeType;
            }

            EntityCombo ec = new EntityCombo();
            Navigator.ConfigureEntityBase(ec, runtimeType, false);

            Common.FireCommonTasks(ec, typeof(T), context);

            helper.ViewContext.HttpContext.Response.Write(
                SetEntityComboOptions(helper, context, ec));
        }

        public static void EntityCombo<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<EntityCombo> settingsModifier)
            where S : Modifiable
        {
            TypeContext<S> context = Common.WalkExpression(tc, property);

            Type runtimeType = typeof(S);
            if (context.Value != null)
            {
                if (typeof(Lazy).IsAssignableFrom(context.Value.GetType()))
                    runtimeType = (context.Value as Lazy).RuntimeType;
                else
                    runtimeType = context.Value.GetType();
            }
            else
            {
                runtimeType = Reflector.ExtractLazy(runtimeType) ?? runtimeType;
            }

            EntityCombo ec = new EntityCombo();
            Navigator.ConfigureEntityBase(ec, runtimeType, false);
            
            Common.FireCommonTasks(ec, typeof(T), context);

            settingsModifier(ec);

            helper.ViewContext.HttpContext.Response.Write(
                SetEntityComboOptions(helper, context, ec));
        }

        private static string SetEntityComboOptions<S>(HtmlHelper helper, TypeContext<S> context, EntityCombo ec)
        {
            if (ec != null)
                using (ec)
                    return helper.InternalEntityCombo(context.Name, typeof(S), context.Value, ec);
            else
                return helper.InternalEntityCombo(context.Name, typeof(S), context.Value, ec);
        }
    }
}
