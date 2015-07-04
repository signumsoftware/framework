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

namespace Signum.Web
{
    public static class EntityListHelper
    {
        private static MvcHtmlString InternalEntityList<T>(this HtmlHelper helper, EntityList entityList)
        {
            if (!entityList.Visible || entityList.HideIfNull && entityList.UntypedValue == null)
                return MvcHtmlString.Empty;

            HtmlStringBuilder sb = new HtmlStringBuilder();
            using (sb.SurroundLine(new HtmlTag("div", entityList.Prefix).Class("SF-entity-list SF-control-container")))
            {
                sb.AddLine(helper.Hidden(entityList.Compose(EntityListBaseKeys.ListPresent), ""));

                using (sb.SurroundLine(new HtmlTag("div", entityList.Compose("hidden")).Class("hide")))
                {
                }

                HtmlStringBuilder sbSelect = new HtmlStringBuilder();

                var sbSelectContainer = new HtmlTag("select").Attr("size", "6").Class("form-control")
                    .IdName(entityList.Compose(EntityListBaseKeys.List));

                if (entityList.ListHtmlProps.Any())
                    sbSelectContainer.Attrs(entityList.ListHtmlProps);

                using (sbSelect.SurroundLine(sbSelectContainer))
                {
                    if (entityList.UntypedValue != null)
                    {
                        foreach (var itemTC in TypeContextUtilities.TypeElementContext((TypeContext<MList<T>>)entityList.Parent))
                            sb.Add(InternalListElement(helper, sbSelect, itemTC, entityList));
                    }
                }

                using (sb.SurroundLine(new HtmlTag("div", entityList.Compose("inputGroup")).Class("input-group")))
                {
                    sb.Add(sbSelect.ToHtml());

                    using (sb.SurroundLine(new HtmlTag("span", entityList.Compose("shownButton")).Class("input-group-btn btn-group-vertical")))
                    {
                        sb.AddLine(EntityButtonHelper.Create(helper, entityList, btn: true));
                        sb.AddLine(EntityButtonHelper.Find(helper, entityList, btn: true));
                        sb.AddLine(EntityButtonHelper.View(helper, entityList, btn: true));
                        sb.AddLine(EntityButtonHelper.Remove(helper, entityList, btn: true));
                        sb.AddLine(EntityButtonHelper.MoveUp(helper, entityList, btn: true));
                        sb.AddLine(EntityButtonHelper.MoveDown(helper, entityList, btn: true));
                    }
                }

                if (entityList.ElementType.IsEmbeddedEntity() && entityList.Create)
                {
                    T embedded = (T)(object)new ConstructorContext(helper.ViewContext.Controller).ConstructUntyped(typeof(T));
                    TypeElementContext<T> templateTC = new TypeElementContext<T>(embedded, (TypeContext)entityList.Parent, 0, null);
                    sb.AddLine(EntityBaseHelper.EmbeddedTemplate(entityList, EntityBaseHelper.RenderPopup(helper, templateTC, RenderPopupMode.Popup, entityList, isTemplate: true), null));
                }
                sb.AddLine(entityList.ConstructorScript(JsModule.Lines, "EntityList"));
            }

            return helper.FormGroup(entityList, entityList.Prefix, entityList.LabelHtml ?? entityList.LabelText.FormatHtml(), sb.ToHtml());
        }

        static MvcHtmlString InternalListElement<T>(this HtmlHelper helper, HtmlStringBuilder sbOptions, TypeElementContext<T> itemTC, EntityList entityList)
        {
            HtmlStringBuilder sb = new HtmlStringBuilder();


            sb.AddLine(EntityBaseHelper.WriteIndex(helper, itemTC));
            sb.AddLine(helper.HiddenRuntimeInfo(itemTC));

            if (entityList.IsVisible == null || entityList.IsVisible(itemTC))
            {
                if (EntityBaseHelper.EmbeddedOrNew((Modifiable)(object)itemTC.Value))
                    sb.AddLine(EntityBaseHelper.RenderPopup(helper, itemTC, RenderPopupMode.PopupInDiv, entityList));
                else if (itemTC.Value != null)
                    sb.Add(helper.Div(itemTC.Compose(EntityBaseKeys.Entity), null, "",
                        new Dictionary<string, object> { { "style", "display:none" }, { "class", "sf-entity-list" } }));

                var optionTag = new HtmlTag("option")
                        .Id(itemTC.Compose(EntityBaseKeys.ToStr))
                        .Class("sf-entity-list-option")
                        .Let(a => itemTC.Index > 0 ? a : a.Attr("selected", "selected"))
                        .SetInnerText(itemTC.Value?.ToString());

                if (!EntityBaseHelper.EmbeddedOrNew((Modifiable)(object)itemTC.Value))
                {
                    PrimaryKey? idOrNull = null;
                    Type type = itemTC.Value.GetType();
                    if (type.IsLite())
                        idOrNull = ((Lite<IEntity>)itemTC.Value).IdOrNull;

                    if (type.IsEntity())
                        idOrNull = ((Entity)(object)itemTC.Value).IdOrNull;

                    optionTag.Attr("title", " ".CombineIfNotEmpty(itemTC.Value.GetType().CleanType().NiceName(), idOrNull));
                }

                sbOptions.Add(optionTag.ToHtml(TagRenderMode.Normal));
            }

            return sb.ToHtml();
        }

        public static MvcHtmlString EntityList<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property)
        {
            return helper.EntityList<T, S>(tc, property, null);
        }

        public static MvcHtmlString EntityList<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property, Action<EntityList> settingsModifier)
        {
            TypeContext<MList<S>> context = Common.WalkExpression(tc, property);

            var vo = tc.ViewOverrides;

            if (vo != null && !vo.IsVisible(context.PropertyRoute))
                return vo.OnSurroundLine(context.PropertyRoute, helper, tc, null);

            EntityList el = new EntityList(context.Type, context.UntypedValue, context, null, context.PropertyRoute);

            EntityBaseHelper.ConfigureEntityBase(el, typeof(S).CleanType());

            Common.FireCommonTasks(el);

            if (settingsModifier != null)
                settingsModifier(el);

            var result = helper.InternalEntityList<S>(el);

            if (vo == null)
                return result;

            return vo.OnSurroundLine(el.PropertyRoute, helper, tc, result);
        }
    }
}
