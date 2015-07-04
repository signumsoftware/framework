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
    public static class EntityListDetailHelper
    {
        private static MvcHtmlString InternalEntityListDetail<T>(this HtmlHelper helper, EntityListDetail listDetail)
        {
            if (!listDetail.Visible || listDetail.HideIfNull && listDetail.UntypedValue == null)
                return MvcHtmlString.Empty;

            HtmlStringBuilder sb = new HtmlStringBuilder();
            using (sb.SurroundLine(new HtmlTag("div", listDetail.Prefix).Class("SF-entity-list-detail SF-control-container")))
            {
                sb.AddLine(helper.Hidden(listDetail.Compose(EntityListBaseKeys.ListPresent), ""));

                using (sb.SurroundLine(new HtmlTag("div", listDetail.Compose("hidden")).Class("hide")))
                {
                }

                HtmlStringBuilder sbSelect = new HtmlStringBuilder();

                var sbSelectContainer = new HtmlTag("select").Attr("size", "6").Class("form-control")
                    .IdName(listDetail.Compose(EntityListBaseKeys.List));

                if (listDetail.ListHtmlProps.Any())
                    sbSelectContainer.Attrs(listDetail.ListHtmlProps);

                using (sbSelect.SurroundLine(sbSelectContainer))
                {
                    if (listDetail.UntypedValue != null)
                    {
                        foreach (var itemTC in TypeContextUtilities.TypeElementContext((TypeContext<MList<T>>)listDetail.Parent))
                            sb.Add(InternalListDetailElement(helper, sbSelect, itemTC, listDetail));
                    }
                }

                using (sb.SurroundLine(new HtmlTag("div", listDetail.Compose("inputGroup")).Class("input-group")))
                {
                    sb.Add(sbSelect.ToHtml());

                    using (sb.SurroundLine(new HtmlTag("span", listDetail.Compose("shownButton")).Class("input-group-btn btn-group-vertical")))
                    {
                        sb.AddLine(EntityButtonHelper.Create(helper, listDetail, btn: true));
                        sb.AddLine(EntityButtonHelper.Find(helper, listDetail, btn: true));
                        sb.AddLine(EntityButtonHelper.Remove(helper, listDetail, btn: true));
                        sb.AddLine(EntityButtonHelper.MoveUp(helper, listDetail, btn: true));
                        sb.AddLine(EntityButtonHelper.MoveDown(helper, listDetail, btn: true));
                    }
                }

                if (listDetail.ElementType.IsEmbeddedEntity() && listDetail.Create)
                {
                    T embedded = (T)(object)new ConstructorContext(helper.ViewContext.Controller).ConstructUntyped(typeof(T));
                    TypeElementContext<T> templateTC = new TypeElementContext<T>(embedded, (TypeContext)listDetail.Parent, 0, null);
                    sb.AddLine(EntityBaseHelper.EmbeddedTemplate(listDetail, EntityBaseHelper.RenderContent(helper, templateTC, RenderContentMode.Content, listDetail), templateTC.Value.ToString()));
                }

                sb.AddLine(listDetail.ConstructorScript(JsModule.Lines, "EntityListDetail"));
            }

            var formGroup = helper.FormGroup(listDetail, listDetail.Prefix, listDetail.LabelHtml ?? listDetail.LabelText.FormatHtml(), sb.ToHtml());

            if (listDetail.DetailDiv != listDetail.Compose(EntityBaseKeys.Detail))
                return formGroup;

            HtmlStringBuilder sb2 = new HtmlStringBuilder();
            sb2.Add(formGroup);
            using (sb2.SurroundLine(new HtmlTag("fieldset")))
                sb2.AddLine(helper.Div(listDetail.DetailDiv, null, "SF-entity-list-detail-detaildiv"));

            return sb2.ToHtml();
        }

        private static MvcHtmlString InternalListDetailElement<T>(this HtmlHelper helper, HtmlStringBuilder sbOptions, TypeElementContext<T> itemTC, EntityListDetail listDetail)
        {
            HtmlStringBuilder sb = new HtmlStringBuilder();

            sb.AddLine(EntityBaseHelper.WriteIndex(helper, itemTC));
            sb.AddLine(helper.HiddenRuntimeInfo(itemTC));

            if (EntityBaseHelper.EmbeddedOrNew((Modifiable)(object)itemTC.Value))
                sb.AddLine(EntityBaseHelper.RenderContent(helper, itemTC, RenderContentMode.ContentInInvisibleDiv, listDetail));
            else if (itemTC.Value != null)
                sb.Add(helper.Div(itemTC.Compose(EntityBaseKeys.Entity), null, "", new Dictionary<string, object> { { "style", "display:none" } }));

            sbOptions.Add(new HtmlTag("option")
                    .Id(itemTC.Compose(EntityBaseKeys.ToStr))
                    .Class("sf-entity-list-option")
                    .Let(a => itemTC.Index > 0 ? a : a.Attr("selected", "selected"))
                    .SetInnerText(itemTC.Value?.ToString())
                    .ToHtml(TagRenderMode.Normal));

            return sb.ToHtml();
        }

        public static MvcHtmlString EntityListDetail<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property)
        {
            return helper.EntityListDetail<T, S>(tc, property, null);
        }

        public static MvcHtmlString EntityListDetail<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property, Action<EntityListDetail> settingsModifier)
        {
            TypeContext<MList<S>> context = Common.WalkExpression(tc, property);

            var vo = tc.ViewOverrides;

            if (vo != null && !vo.IsVisible(context.PropertyRoute))
                return vo.OnSurroundLine(context.PropertyRoute, helper, tc, null);

            EntityListDetail eld = new EntityListDetail(context.Type, context.UntypedValue, context, null, context.PropertyRoute);

            EntityBaseHelper.ConfigureEntityBase(eld, typeof(S).CleanType());

            Common.FireCommonTasks(eld);

            if (settingsModifier != null)
                settingsModifier(eld);

            var result = helper.InternalEntityListDetail<S>(eld);

            if (vo == null)
                return result;

            return vo.OnSurroundLine(eld.PropertyRoute, helper, tc, result);
        }
    }
}
