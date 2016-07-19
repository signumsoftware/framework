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

namespace Signum.Web
{
    public static class EntityComboHelper
    {
        internal static MvcHtmlString InternalEntityCombo(this HtmlHelper helper, EntityCombo entityCombo)
        {
            if (!entityCombo.Visible || entityCombo.HideIfNull && entityCombo.UntypedValue == null)
                return MvcHtmlString.Empty;

            if (!entityCombo.Type.IsIEntity() && !entityCombo.Type.IsLite())
                throw new InvalidOperationException("EntityCombo can only be done for an identifiable or a lite, not for {0}".FormatWith(entityCombo.Type.CleanType()));

            HtmlStringBuilder sb = new HtmlStringBuilder();
            using (sb.SurroundLine(new HtmlTag("div", entityCombo.Prefix).Class("SF-entity-combo SF-control-container")))
            {
                sb.AddLine(helper.HiddenRuntimeInfo(entityCombo));

                using (sb.SurroundLine(new HtmlTag("div", entityCombo.Compose("hidden")).Class("hide")))
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

                using (sb.SurroundLine(new HtmlTag("div", entityCombo.Compose("inputGroup")).Class("input-group")))
                {
                    if (entityCombo.ReadOnly)
                        sb.AddLine(helper.FormControlStatic(entityCombo, entityCombo.Compose(EntityBaseKeys.ToStr), entityCombo.UntypedValue?.ToString()));
                    else
                        sb.AddLine(DropDownList(helper, entityCombo));

                    using (sb.SurroundLine(new HtmlTag("span", entityCombo.Compose("shownButton")).Class("input-group-btn")))
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
                    EmbeddedEntity embedded = (EmbeddedEntity)new ConstructorContext(helper.ViewContext.Controller).ConstructUntyped(entityCombo.Type.CleanType());
                    TypeContext templateTC = ((TypeContext)entityCombo.Parent).Clone(embedded);
                    sb.AddLine(EntityBaseHelper.EmbeddedTemplate(entityCombo, EntityBaseHelper.RenderPopup(helper, templateTC, RenderPopupMode.Popup, entityCombo, isTemplate: true), null));
                }

                if (EntityBaseHelper.EmbeddedOrNew((Modifiable)entityCombo.UntypedValue))
                    sb.AddLine(EntityBaseHelper.RenderPopup(helper, (TypeContext)entityCombo.Parent, RenderPopupMode.PopupInDiv, entityCombo));

                sb.AddLine(entityCombo.ConstructorScript(JsModule.Lines, "EntityCombo"));
            }

            return helper.FormGroup(entityCombo, entityCombo.Prefix, entityCombo.LabelHtml ?? entityCombo.LabelText.FormatHtml(), sb.ToHtml());
        }

        private static MvcHtmlString DropDownList(HtmlHelper helper, EntityCombo entityCombo)
        {
            List<SelectListItem> items = new List<SelectListItem>();
            items.Add(new SelectListItem() { Text = "-", Value = "" });

            List<Lite<IEntity>> data = entityCombo.Data != null ? entityCombo.Data.ToList() :
                AutocompleteUtils.FindAllLite(entityCombo.Implementations.Value).Cast<Lite<IEntity>>().ToList();

            if (entityCombo.SortElements)
                data = data.OrderBy(a => a.ToString()).ToList();

            var current = entityCombo.UntypedValue is IEntity ? ((IEntity)entityCombo.UntypedValue)?.ToLite() :
                entityCombo.UntypedValue as Lite<IEntity>;

            if (current != null && !data.Contains(current))
                data.Add(current);

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

            return helper.SafeDropDownList(
                    entityCombo.Compose(EntityComboKeys.Combo),
                    items,
                    entityCombo.ComboHtmlProperties);
        }

        public static MvcHtmlString EntityCombo<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property) 
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

        public static SelectListItem ToSelectListItem<T>(this Lite<T> lite, Lite<T> selected) where T : class, IEntity
        {
            return new SelectListItem { Text = lite.ToString(), Value = lite.Id.ToString(), Selected = selected.Is(lite) };
        }

        public static MvcHtmlString ToOptions<T>(this IEnumerable<Lite<T>> lites, Lite<T> selectedElement) where T : class, IEntity
        {
            List<SelectListItem> list = new List<SelectListItem>();

            if (selectedElement == null || !lites.Contains(selectedElement))
                list.Add(new SelectListItem { Text = "-", Value = "" });

            list.AddRange(lites.Select(l => l.ToSelectListItem(selectedElement)));

            return new HtmlStringBuilder(list.Select(RenderOption)).ToHtml();
        }
    }
}
