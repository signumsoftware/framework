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
using Signum.Engine;

namespace Signum.Web
{
    public static class EntityTabRepeaterHelper
    {
        private static MvcHtmlString InternalEntityRepeater<T>(this HtmlHelper helper, EntityTabRepeater repeater)
        {
            if (!repeater.Visible || repeater.HideIfNull && repeater.UntypedValue == null)
                return MvcHtmlString.Empty;

            HtmlStringBuilder sb = new HtmlStringBuilder();

            using (sb.SurroundLine(new HtmlTag("fieldset").Id(repeater.Prefix).Class("sf-tab-repeater-field SF-control-container SF-avoid-child-errors")))
            {
                using (sb.SurroundLine(new HtmlTag("legend")))
                using (sb.SurroundLine(new HtmlTag("div", repeater.Compose("header"))))
                {
                    sb.AddLine(new HtmlTag("span").InnerHtml(repeater.LabelHtml ?? repeater.LabelText.FormatHtml()).ToHtml());

                    using (sb.SurroundLine(new HtmlTag("span", repeater.Compose("shownButton")).Class("pull-right")))
                    {
                        sb.AddLine(EntityButtonHelper.Create(helper, repeater, btn: false));
                        sb.AddLine(EntityButtonHelper.Find(helper, repeater, btn: false));
                    }
                }

                sb.AddLine(helper.Hidden(repeater.Compose(EntityListBaseKeys.ListPresent), ""));

                using (sb.SurroundLine(new HtmlTag("div")))
                {
                    using (sb.SurroundLine(new HtmlTag("ul", repeater.Compose(EntityRepeaterKeys.ItemsContainer)).Class("nav nav-tabs")))
                    {
                        if (repeater.UntypedValue != null)
                        {
                            foreach (var itemTC in TypeContextUtilities.TypeElementContext((TypeContext<MList<T>>)repeater.Parent))
                                sb.Add(InternalTabRepeaterHeader(helper, itemTC, repeater));
                        }
                    }

                    using (sb.SurroundLine(new HtmlTag("div", repeater.Compose(EntityRepeaterKeys.TabsContainer)).Class("tab-content")))
                        if (repeater.UntypedValue != null)
                        {
                            foreach (var itemTC in TypeContextUtilities.TypeElementContext((TypeContext<MList<T>>)repeater.Parent))
                                using (sb.SurroundLine(new HtmlTag("div", itemTC.Compose(EntityBaseKeys.Entity)).Class("tab-pane")
                                    .Let(h => itemTC.Index == 0 ? h.Class("active") : h)))
                                    sb.Add(EntityBaseHelper.RenderContent(helper, itemTC, RenderContentMode.Content, repeater));
                        }
                }

                if (repeater.ElementType.IsEmbeddedEntity() && repeater.Create)
                {
                    T embedded = (T)(object)new ConstructorContext(helper.ViewContext.Controller).ConstructUntyped(typeof(T));
                    TypeElementContext<T> templateTC = new TypeElementContext<T>(embedded, (TypeContext)repeater.Parent, 0, null);
                    sb.AddLine(EntityBaseHelper.EmbeddedTemplate(repeater, EntityBaseHelper.RenderContent(helper, templateTC, RenderContentMode.Content, repeater), templateTC.Value.ToString()));
                }

                sb.AddLine(repeater.ConstructorScript(JsModule.Lines, "EntityTabRepeater"));
            }

            return sb.ToHtml();
        }

        private static MvcHtmlString InternalTabRepeaterHeader<T>(this HtmlHelper helper, TypeElementContext<T> itemTC, EntityTabRepeater repeater)
        {
            HtmlStringBuilder sb = new HtmlStringBuilder();

            using (sb.SurroundLine(new HtmlTag("li", itemTC.Compose(EntityRepeaterKeys.RepeaterElement)).Let(h => itemTC.Index == 0 ? h.Class("active") : h)
                .Class("sf-repeater-element")))
            {
                using (sb.SurroundLine(new HtmlTag("a")
                    .Attr("href", "#" + itemTC.Compose(EntityBaseKeys.Entity))
                    .Attr("data-toggle", "tab")))
                {
                    sb.Add(new HtmlTag("span").SetInnerText(itemTC.Value.ToString()));

                    sb.AddLine(EntityBaseHelper.WriteIndex(helper, itemTC));
                    sb.AddLine(helper.HiddenRuntimeInfo(itemTC));

                    if (repeater.Move)
                    {
                        sb.AddLine(EntityButtonHelper.MoveUpItem(helper, itemTC, repeater, btn: false, elementType: "span", isVertical: false));
                        sb.AddLine(EntityButtonHelper.MoveDownItem(helper, itemTC, repeater, btn: false, elementType: "span", isVertical: false));
                    }

                    if (repeater.Remove)
                        sb.AddLine(EntityButtonHelper.RemoveItem(helper, itemTC, repeater, btn: false, elementType: "span"));
                }

            }

            return sb.ToHtml();
        }

        public static MvcHtmlString EntityTabRepeater<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property)
            where S : Modifiable
        {
            return helper.EntityTabRepeater(tc, property, null);
        }

        public static MvcHtmlString EntityTabRepeater<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property, Action<EntityTabRepeater> settingsModifier)
        {
            TypeContext<MList<S>> context = Common.WalkExpression(tc, property);

            var vo = tc.ViewOverrides;

            if (vo != null && !vo.IsVisible(context.PropertyRoute))
                return vo.OnSurroundLine(context.PropertyRoute, helper, tc, null);

            EntityTabRepeater repeater = new EntityTabRepeater(context.Type, context.UntypedValue, context, null, context.PropertyRoute);

            EntityBaseHelper.ConfigureEntityBase(repeater, typeof(S).CleanType());

            Common.FireCommonTasks(repeater);

            if (settingsModifier != null)
                settingsModifier(repeater);

            var result = helper.InternalEntityRepeater<S>(repeater);

            if (vo == null)
                return result;

            return vo.OnSurroundLine(repeater.PropertyRoute, helper, tc, result);
        }
    }
}
