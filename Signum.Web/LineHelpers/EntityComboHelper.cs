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
using System.Web;
using Signum.Engine.DynamicQuery;
#endregion

namespace Signum.Web
{
    public static class EntityComboHelper
    {
        internal static MvcHtmlString InternalEntityCombo(this HtmlHelper helper, EntityCombo entityCombo)
        {
            if (!entityCombo.Visible || entityCombo.HideIfNull && entityCombo.UntypedValue == null)
                return MvcHtmlString.Empty;

            if (!entityCombo.Type.IsIIdentifiable() && !entityCombo.Type.IsLite())
                throw new InvalidOperationException("EntityCombo can only be done for an identifiable or a lite, not for {0}".Formato(entityCombo.Type.CleanType()));

            HtmlStringBuilder sb = new HtmlStringBuilder();
            using (sb.Surround(new HtmlTag("div", entityCombo.Prefix).Class("SF-entity-combo SF-control-container")))
            {
                sb.AddLine(helper.HiddenRuntimeInfo(entityCombo));

                using (sb.Surround(new HtmlTag("div", entityCombo.Compose("hidden")).Class("hide")))
                {
                    if (entityCombo.UntypedValue != null)
                    {
                        sb.AddLine(EntityButtonHelper.Create(helper, entityCombo, btn: true));
                        sb.AddLine(EntityButtonHelper.Find(helper, entityCombo, btn: true));
                    }
                    else
                    {
                        sb.AddLine(EntityButtonHelper.View(helper, entityCombo, btn: true));
                        sb.AddLine(EntityButtonHelper.Remove(helper, entityCombo, btn: true));
                    }
                }

                using (sb.Surround(new HtmlTag("div", entityCombo.Compose("inputGroup")).Class("input-group")))
                {
                    if (entityCombo.ReadOnly)
                        sb.AddLine(helper.FormControlStatic(entityCombo.Compose(EntityBaseKeys.ToStr), entityCombo.UntypedValue.TryToString()));
                    else
                        sb.AddLine(DropDownList(helper, entityCombo));

                    using (sb.Surround(new HtmlTag("span", entityCombo.Compose("shownButton")).Class("input-group-btn")))
                    {
                        if (entityCombo.UntypedValue == null)
                        {
                            sb.AddLine(EntityButtonHelper.Create(helper, entityCombo, btn: true));
                            sb.AddLine(EntityButtonHelper.Find(helper, entityCombo, btn: true));
                        }
                        else
                        {
                            sb.AddLine(EntityButtonHelper.View(helper, entityCombo, btn: true));
                            sb.AddLine(EntityButtonHelper.Remove(helper, entityCombo, btn: true));
                        }
                    }
                }

                if (entityCombo.Type.IsEmbeddedEntity() && entityCombo.Create)
                {
                    TypeContext templateTC = ((TypeContext)entityCombo.Parent).Clone((object)Constructor.Construct(entityCombo.Type.CleanType()));
                    sb.AddLine(EntityBaseHelper.EmbeddedTemplate(entityCombo, EntityBaseHelper.RenderPopup(helper, templateTC, RenderPopupMode.Popup, entityCombo, isTemplate: true), null));
                }

                if (EntityBaseHelper.EmbeddedOrNew((Modifiable)entityCombo.UntypedValue))
                    sb.AddLine(EntityBaseHelper.RenderPopup(helper, (TypeContext)entityCombo.Parent, RenderPopupMode.PopupInDiv, entityCombo));

                sb.AddLine(entityCombo.ConstructorScript(JsFunction.LinesModule, "EntityCombo"));
            }

            return helper.FormGroup(entityCombo, entityCombo.Prefix, entityCombo.LabelText, sb.ToHtml());
        }

        private static MvcHtmlString DropDownList(HtmlHelper helper, EntityCombo entityCombo)
        {
            List<SelectListItem> items = new List<SelectListItem>();
            items.Add(new SelectListItem() { Text = "-", Value = "" });

            IEnumerable<Lite<IIdentifiable>> data = entityCombo.Data ?? AutocompleteUtils.FindAllLite(entityCombo.Implementations.Value);


            var current = entityCombo.UntypedValue is IIdentifiable ? ((IIdentifiable)entityCombo.UntypedValue).ToLite() :
                entityCombo.UntypedValue as Lite<IIdentifiable>;

            items.AddRange(
                data.Select(lite => new SelectListItem()
                {
                    Text = lite.ToString(),
                    Value = lite.Key(),
                    Selected = lite.Is(current)
                }));


            entityCombo.ComboHtmlProperties.AddCssClass("form-control");

            if (entityCombo.ComboHtmlProperties.ContainsKey("onchange"))
                throw new InvalidOperationException("EntityCombo cannot have onchange html property, use onEntityChanged instead");

            entityCombo.ComboHtmlProperties.Add("onchange", entityCombo.SFControlThen("combo_selected()"));

            if (entityCombo.Size > 0)
            {
                entityCombo.ComboHtmlProperties.AddCssClass("sf-entity-list");
                entityCombo.ComboHtmlProperties.Add("size", Math.Min(entityCombo.Size, items.Count - 1));
            }

            if (entityCombo.PlaceholderLabels && !entityCombo.ComboHtmlProperties.ContainsKey("placeholder"))
                entityCombo.ComboHtmlProperties.Add("placeholder", entityCombo.LabelText);

            return helper.DropDownList(
                    entityCombo.Compose(EntityComboKeys.Combo),
                    items,
                    entityCombo.ComboHtmlProperties);
        }

        public static MvcHtmlString EntityCombo<T,S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property) 
        {
            return helper.EntityCombo<T, S>(tc, property, null);
        }

        public static MvcHtmlString EntityCombo<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<EntityCombo> settingsModifier)
        {
            TypeContext<S> context = Common.WalkExpression(tc, property);

            var vo = tc.ViewOverrides;

            if (vo != null && !vo.IsVisible(context.PropertyRoute))
                return vo.OnSurroundLine(context.PropertyRoute, helper, tc, null);


            EntityCombo ec = new EntityCombo(typeof(S), context.Value, context, null, context.PropertyRoute);

            EntityBaseHelper.ConfigureEntityBase(ec, ec.CleanRuntimeType ?? ec.Type.CleanType());

            Common.FireCommonTasks(ec);

            if (settingsModifier != null)
                settingsModifier(ec);

            var result = helper.InternalEntityCombo(ec);

            if (vo == null)
                return result;

            return vo.OnSurroundLine(ec.PropertyRoute, helper, tc, result);
        }

        public static MvcHtmlString RenderOption(this SelectListItem item)
        {
            HtmlTag builder = new HtmlTag("option").SetInnerText(item.Text);

            if (item.Value != null)
                builder.Attr("value", item.Value);

            if (item.Selected)
                builder.Attr("selected", "selected");

            return builder.ToHtml();
        }

        public static SelectListItem ToSelectListItem<T>(this Lite<T> lite, Lite<T> selected) where T : class, IIdentifiable
        {
            return new SelectListItem { Text = lite.ToString(), Value = lite.Id.ToString(), Selected = selected.Is(lite) };
        }

        public static MvcHtmlString ToOptions<T>(this IEnumerable<Lite<T>> lites, Lite<T> selectedElement) where T : class, IIdentifiable
        {
            List<SelectListItem> list = new List<SelectListItem>();

            if (selectedElement == null || !lites.Contains(selectedElement))
                list.Add(new SelectListItem { Text = "-", Value = "" });

            list.AddRange(lites.Select(l => l.ToSelectListItem(selectedElement)));

            return new HtmlStringBuilder(list.Select(RenderOption)).ToHtml();
        }
    }
}
