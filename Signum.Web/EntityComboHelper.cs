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

namespace Signum.Web
{
    public static class EntityComboKeys
    {
        public const string Combo = "sfCombo";
    }

    public class EntityCombo : EntityBase
    {
        public EntityCombo()
        {
            Create = false;
            Remove = false;
            Find = false;
        }
    }

    public static class EntityComboHelper
    {

        internal static string InternalEntityCombo(this HtmlHelper helper, string idValueField, Type type, object value, EntityCombo settings)
        {
            idValueField = helper.GlobalName(idValueField);
            string divASustituir = helper.GlobalName("divASustituir");

            StringBuilder sb = new StringBuilder();
            sb.Append(helper.Hidden(idValueField + TypeContext.Separator + TypeContext.StaticType, (Reflector.ExtractLazy(type) ?? type).Name));

            if (StyleContext.Current.LabelVisible)
                sb.Append(helper.Span(idValueField + "lbl", settings.LabelText ?? "", TypeContext.CssLineLabel));

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

                sb.Append(helper.Div(idValueField + TypeContext.Separator + EntityBaseKeys.Entity, "", "", new Dictionary<string, string> { { "style", "display:none" } }));

                List<SelectListItem> items = new List<SelectListItem>();
                items.Add(new SelectListItem() { Text = "-", Value = "", Selected = true });
                items.AddRange(
                    Database.RetrieveAllLazy(Reflector.ExtractLazy(type) ?? type)
                        .Select(lazy => new SelectListItem()
                        {
                            Text = lazy.ToString(),
                            Value = lazy.Id.ToString(),
                            Selected = (value!=null) && (lazy.Id == ((IIdentifiable)(object)value).TryCS(i => i.Id))
                        })
                    );

                sb.Append(helper.DropDownList(
                    idValueField + TypeContext.Separator + EntityComboKeys.Combo,
                    items,
                    new Dictionary<string, object> 
                    { 
                        {"class","valueLine"},
                        {"onchange","EntityComboOnChange('{0}');".Formato(idValueField)}
                    }));
                sb.Append("\n");
            }

            string viewingUrl = "javascript:OpenPopup('/Signum/PartialView','{0}','{1}',function(){{OnPopupComboOk('/Signum/TrySavePartial','{1}');}},function(){{OnPopupComboCancel('{1}');}});".Formato(divASustituir, idValueField);
            sb.Append(helper.Button(
                        idValueField + TypeContext.Separator + "_btnView",
                        "->",
                        viewingUrl,
                        "",
                        new Dictionary<string, string> { {"style","display:" + ((value==null) ? "none" : "block")}}));

            sb.Append("<script type=\"text/javascript\">var " + idValueField + "_sfEntityTemp = \"\"</script>\n");
            
            if (StyleContext.Current.BreakLine)
                sb.Append("<div class=\"clearall\"></div>\n");

            return sb.ToString();
        }

        public static void EntityCombo<T,S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property) 
            where S : Modifiable 
        {
            TypeContext<S> context = Common.WalkExpressionGen(tc, property);

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
            Common.FireCommonTasks(ec, typeof(T), context);

            //if (ec.Implementations == null)
            //    Navigator.ConfigureEntityBase(el, runtimeType , false);

            helper.ViewContext.HttpContext.Response.Write(
                helper.InternalEntityCombo(context.Name, typeof(S), context.Value, ec));
        }

        public static void EntityCombo<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<EntityCombo> settingsModifier)
            where S : Modifiable
        {
            TypeContext<S> context = Common.WalkExpressionGen(tc, property);

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
            Common.FireCommonTasks(ec, typeof(T), context);

            //if (el.Implementations == null)
            //    Navigator.ConfigureEntityBase(el, runtimeType, false);

            settingsModifier(ec);

            helper.ViewContext.HttpContext.Response.Write(
                helper.InternalEntityCombo(context.Name, typeof(S), context.Value, ec));
        }

    }


}
