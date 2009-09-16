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
            sb.Append(helper.Hidden(idValueField + TypeContext.Separator + TypeContext.StaticType, (Reflector.ExtractLazy(type) ?? type).Name));

            if (StyleContext.Current.LabelVisible)
                sb.Append(helper.Label(idValueField + "lbl", settings.LabelText ?? "", idValueField+ "_" + EntityComboKeys.Combo, TypeContext.CssLineLabel));

            string runtimeType = "";
            if (value != null)
            {
                Type cleanRuntimeType = value.GetType();
                if (typeof(Lazy).IsAssignableFrom(value.GetType()))
                    cleanRuntimeType = (value as Lazy).RuntimeType;
                runtimeType = cleanRuntimeType.Name;
            }
            sb.Append(helper.Hidden(idValueField + TypeContext.Separator + TypeContext.RuntimeType, runtimeType));
                
            bool isIdentifiable = typeof(IdentifiableEntity).IsAssignableFrom(type);
            bool isLazy = typeof(Lazy).IsAssignableFrom(type);
            if (isIdentifiable || isLazy)
            {
                sb.Append(helper.Hidden(
                    idValueField + TypeContext.Separator + TypeContext.Id, 
                    (isIdentifiable) 
                       ? ((IIdentifiable)(object)value).TryCS(i => i.Id).TrySS(id => id)
                       : ((Lazy)(object)value).TryCS(i => i.Id).TrySS(id => id)) + "\n");

                sb.Append(helper.Div(idValueField + TypeContext.Separator + EntityBaseKeys.Entity, "", "", new Dictionary<string, object> { { "style", "display:none" } }));

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

                    sb.Append(helper.DropDownList(
                        idValueField + TypeContext.Separator + EntityComboKeys.Combo,
                        items,
                        settings.ComboHtmlProperties));
                    sb.Append("\n");
                }
                else
                {
                    sb.Append(helper.Span(idValueField, (value!=null) ? value.ToString() : "", "valueLine"));
                }
            }

            if (settings.View)
            {
                string viewingUrl = "javascript:OpenPopup('{0}','{1}','{2}',function(){{OnPopupComboOk('{3}','{2}');}},function(){{OnPopupComboCancel('{2}');}});".Formato("Signum.aspx/PopupView", divASustituir, idValueField, "Signum.aspx/ValidatePartial");
                sb.Append(helper.Button(
                            idValueField + TypeContext.Separator + "_btnView",
                            "->",
                            viewingUrl,
                            "lineButton go",
                            new Dictionary<string, object> { { "style", "display:" + ((value == null) ? "none" : "inline") } }));
            }

            if (settings.Create)
            {
                string creatingUrl = "javascript:NewPopup('{0}','{1}','{2}',function(){{OnPopupComboOk('{3}','{2}');}},function(){{OnPopupComboCancel('{2}');}});".Formato("Signum.aspx/PopupView", divASustituir, idValueField, "Signum.aspx/ValidatePartial", (typeof(EmbeddedEntity).IsAssignableFrom(type)));
                sb.Append(
                    helper.Button(idValueField + "_btnCreate",
                              "+",
                              creatingUrl,
                              "lineButton create",
                              (value == null) ? new Dictionary<string, object>() : new Dictionary<string, object>() { { "style", "display:none" } }));
            }

            sb.Append("<script type=\"text/javascript\">var " + idValueField + "_sfEntityTemp = \"\"</script>\n");
            
            if (StyleContext.Current.BreakLine)
                sb.Append("<div class=\"clearall\"></div>\n");

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
            //if (ec != null && ec.StyleContext != null)
            //{
            //    using (ec.StyleContext)
            //        return helper.InternalEntityCombo(context.Name, typeof(S), context.Value, ec);
            //}
            //else
            //    return helper.InternalEntityCombo(context.Name, typeof(S), context.Value, ec);
            if (ec != null)
                using (ec)
                    return helper.InternalEntityCombo(context.Name, typeof(S), context.Value, ec);
            else
                return helper.InternalEntityCombo(context.Name, typeof(S), context.Value, ec);
        }

    }


}
