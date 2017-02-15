using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Collections;
using System.Linq.Expressions;
using System.Web.Mvc.Html;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities;
using System.Configuration;
using Signum.Engine.DynamicQuery;
using Signum.Engine;

namespace Signum.Web
{
    public static class EntityListCheckboxHelper
    {
        private static MvcHtmlString InternalEntityListCheckbox<T>(this HtmlHelper helper, EntityListCheckbox entityListCheckBox)
        {
            if (!entityListCheckBox.Visible || entityListCheckBox.HideIfNull && entityListCheckBox.UntypedValue == null)
                return MvcHtmlString.Empty;

            var elementType = entityListCheckBox.Type.ElementType();

            if (!elementType.IsIEntity() && !elementType.IsLite())
                throw new InvalidOperationException("EntityCombo can only be done for an identifiable or a lite, not for {0}".FormatWith(elementType.CleanType()));

            HtmlStringBuilder sb = new HtmlStringBuilder();
            using (sb.SurroundLine(new HtmlTag("fieldset", entityListCheckBox.Prefix).Class("SF-repeater-field SF-control-container SF-avoid-child-errors")))
            {
                sb.AddLine(helper.Hidden(entityListCheckBox.Compose(EntityListBaseKeys.ListPresent), ""));

                using (sb.SurroundLine(new HtmlTag("div", entityListCheckBox.Compose("hidden")).Class("hide")))
                {
                }

                using (sb.SurroundLine(new HtmlTag("legend")))
                using (sb.SurroundLine(new HtmlTag("div", entityListCheckBox.Compose("header"))))
                {
                    sb.AddLine(new HtmlTag("span").InnerHtml(entityListCheckBox.LabelHtml ?? entityListCheckBox.LabelText.FormatHtml()).ToHtml());

                    using (sb.SurroundLine(new HtmlTag("span", entityListCheckBox.Compose("shownButton")).Class("pull-right")))
                    {
                        sb.AddLine(EntityButtonHelper.Create(helper, entityListCheckBox, btn: false));
                        sb.AddLine(EntityButtonHelper.Find(helper, entityListCheckBox, btn: false));
                    }
                }

                using (sb.SurroundLine(new HtmlTag("div").Id(entityListCheckBox.Compose(EntityRepeaterKeys.ItemsContainer)).Attr("style", GetStyle(entityListCheckBox))))
                {
                    IEnumerable<Lite<IEntity>> data = entityListCheckBox.Data ?? AutocompleteUtils.FindAllLite(entityListCheckBox.Implementations.Value).OrderBy(a => a.ToString());

                    if (entityListCheckBox.UntypedValue != null)
                    {
                        var already = TypeContextUtilities.TypeElementContext((TypeContext<MList<T>>)entityListCheckBox.Parent).ToDictionaryEx(a=>AsLite(a.Value), "repeated elements");

                        List<Lite<IEntity>> liteList = data.Except(already.Keys).ToList();
                        
                        List<T> typedList = typeof(Lite<IEntity>).IsAssignableFrom(typeof(T)) ? liteList.Cast<T>().ToList(): Database.RetrieveFromListOfLite(liteList).Cast<T>().ToList();                        
                        
                        var extra = typedList.Select((e,i)=>new TypeElementContext<T>(e, (TypeContext)entityListCheckBox.Parent, i + already.Count, null)).ToDictionaryEx(a=>AsLite(a.Value), "repeated elements");

                        foreach (var lite in data)
                            sb.Add(InternalRepeaterElement(helper, already.TryGetC(lite) ?? extra.GetOrThrow(lite), entityListCheckBox, already.ContainsKey(lite), lite));
                    }
                }

                if (entityListCheckBox.ElementType.IsEmbeddedEntity() && entityListCheckBox.Create)
                {
                    T embedded = (T)(object)new ConstructorContext(helper.ViewContext.Controller).ConstructUntyped(typeof(T));
                    TypeElementContext<T> templateTC = new TypeElementContext<T>(embedded, (TypeContext)entityListCheckBox.Parent, 0, null);
                    sb.AddLine(EntityBaseHelper.EmbeddedTemplate(entityListCheckBox, EntityBaseHelper.RenderContent(helper, templateTC, RenderContentMode.Content, entityListCheckBox), null));
                }

                sb.AddLine(entityListCheckBox.ConstructorScript(JsModule.Lines, "EntityListCheckbox"));
            }

            return sb.ToHtml();
        }

        private static string GetStyle(EntityListCheckbox entityListCheckBox)
        {
            if (entityListCheckBox.ColumnCount.HasValue && entityListCheckBox.ColumnWidth.HasValue)
                return "-webkit-columns: {0} {1}px; -moz-columns: {0} {1}px; columns: {0} {1}px;".FormatWith(entityListCheckBox.ColumnCount.Value, entityListCheckBox.ColumnWidth.Value);

            if(entityListCheckBox.ColumnCount.HasValue)
                return "-webkit-column-count: {0}; -moz-column-count: {0};column-count: {0};".FormatWith(entityListCheckBox.ColumnCount.Value);

            if (entityListCheckBox.ColumnWidth.HasValue)
                return "-webkit-column-width: {0}px;-moz-column-width: {0}px; column-width: {0}px;".FormatWith(entityListCheckBox.ColumnWidth.Value);

            return null;
        }

        private static Lite<IEntity> AsLite(object value)
        {
            return value is IEntity ? ((IEntity)value).ToLite() : (Lite<IEntity>)value;
        }

        private static MvcHtmlString InternalRepeaterElement<T>(this HtmlHelper helper, TypeElementContext<T> itemTC, EntityListCheckbox entityListCheckBox, bool isChecked, Lite<IEntity> lite)
        {
            HtmlStringBuilder sb = new HtmlStringBuilder();

            var label = new HtmlTag("label", itemTC.Compose(EntityRepeaterKeys.RepeaterElement)).Class("sf-checkbox-element");

            entityListCheckBox.CustomizeLabel?.Invoke(label, lite);

            using (sb.SurroundLine(label))
            {
                if (EntityBaseHelper.EmbeddedOrNew((Modifiable)(object)itemTC.Value))
                    sb.AddLine(EntityBaseHelper.RenderPopup(helper, itemTC, RenderPopupMode.PopupInDiv, entityListCheckBox));
                else if (itemTC.Value != null)
                    sb.Add(helper.Div(itemTC.Compose(EntityBaseKeys.Entity), null, "",
                        new Dictionary<string, object> { { "style", "display:none" }, { "class", "sf-entity-list" } }));

                var cb = new HtmlTag("input")
                    .Attr("type", "checkbox")
                    .Attr("name", itemTC.Compose(EntityBaseKeys.RuntimeInfo))
                    .Attr("value", itemTC.RuntimeInfo()?.ToString());

                if(isChecked)
                    cb.Attr("checked", "checked");

                if (entityListCheckBox.ReadOnly)
                    cb.Attr("disabled", "disabled");
                
                entityListCheckBox.CustomizeCheckBox?.Invoke(cb, lite);

                sb.AddLine(cb);

                if (lite != null && (entityListCheckBox.Navigate || entityListCheckBox.View))
                {
                    var dic = new Dictionary<string, object>
                    {
                        { "target", "_blank"}
                    };

                    sb.AddLine(
                        helper.Href(itemTC.Compose(EntityBaseKeys.Link),
                        lite.ToString(),
                        lite.IdOrNull == null ? null : Navigator.NavigateRoute(lite),
                        lite.ToString(), "sf-entitStrip-link", dic));
                }
                else
                {
                    sb.AddLine(
                        helper.Span(itemTC.Compose(EntityBaseKeys.Link),
                            itemTC.UntypedValue.ToString() ?? " ", "sf-entitStrip-link"));
                }
            }

            return sb.ToHtml();
        }

        public static MvcHtmlString EntityListCheckbox<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property)
        {
            return helper.EntityListCheckbox<T, S>(tc, property, null);
        }

        public static MvcHtmlString EntityListCheckbox<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property, Action<EntityListCheckbox> settingsModifier)
        {
            TypeContext<MList<S>> context = Common.WalkExpression(tc, property);

            var vo = tc.ViewOverrides;

            if (vo != null && !vo.IsVisible(context.PropertyRoute))
                return vo.OnSurroundLine(context.PropertyRoute, helper, tc, null);

            EntityListCheckbox el = new EntityListCheckbox(context.Type, context.UntypedValue, context, null, context.PropertyRoute);

            EntityBaseHelper.ConfigureEntityBase(el, typeof(S).CleanType());

            Common.FireCommonTasks(el);

            if (settingsModifier != null)
                settingsModifier(el);

            var result = helper.InternalEntityListCheckbox<S>(el);

            if (vo == null)
                return result;

            return vo.OnSurroundLine(el.PropertyRoute, helper, tc, result);
        }
    }
}
