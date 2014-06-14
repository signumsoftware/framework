#region usings
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
#endregion

namespace Signum.Web
{
    public static class EntityTabRepeaterHelper
    {
        private static MvcHtmlString InternalEntityRepeater<T>(this HtmlHelper helper, EntityTabRepeater repeater)
        {
            if (!repeater.Visible || repeater.HideIfNull && repeater.UntypedValue == null)
                return MvcHtmlString.Empty;

            HtmlStringBuilder sb = new HtmlStringBuilder();

            using (sb.Surround(new HtmlTag("fieldset").Id(repeater.Prefix).Class("sf-tab-repeater-field SF-control-container SF-avoid-child-errors")))
            {
                using (sb.Surround(new HtmlTag("legend")))
                using (sb.Surround(new HtmlTag("div", repeater.Compose("header"))))
                {
                    sb.AddLine(new HtmlTag("span").SetInnerText(repeater.LabelText).ToHtml());

                    using (sb.Surround(new HtmlTag("span", repeater.Compose("shownButton")).Class("pull-right")))
                    {
                        sb.AddLine(EntityButtonHelper.Create(helper, repeater, btn: false));
                        sb.AddLine(EntityButtonHelper.Find(helper, repeater, btn: false));
                    }
                }

                sb.AddLine(helper.Hidden(repeater.Compose(EntityListBaseKeys.ListPresent), ""));

                using (sb.Surround(new HtmlTag("div")))
                {
                    using (sb.Surround(new HtmlTag("ul", repeater.Compose(EntityRepeaterKeys.ItemsContainer)).Class("nav nav-tabs")))
                    {
                        if (repeater.UntypedValue != null)
                        {
                            foreach (var itemTC in TypeContextUtilities.TypeElementContext((TypeContext<MList<T>>)repeater.Parent))
                                sb.Add(InternalTabRepeaterHeader(helper, itemTC, repeater));
                        }
                    }

                    using (sb.Surround(new HtmlTag("div", repeater.Compose(EntityRepeaterKeys.TabsContainer)).Class("tab-content")))
                        if (repeater.UntypedValue != null)
                        {
                            foreach (var itemTC in TypeContextUtilities.TypeElementContext((TypeContext<MList<T>>)repeater.Parent))
                                using (sb.Surround(new HtmlTag("div", itemTC.Compose(EntityBaseKeys.Entity)).Class("tab-pane")
                                    .Let(h => itemTC.Index == 0 ? h.Class("active") : h)))
                                    sb.Add(EntityBaseHelper.RenderContent(helper, itemTC, RenderContentMode.Content, repeater));
                        }
                }

                if (repeater.ElementType.IsEmbeddedEntity() && repeater.Create)
                {
                    TypeElementContext<T> templateTC = new TypeElementContext<T>((T)(object)helper.ViewContext.Controller.Construct(typeof(T)), (TypeContext)repeater.Parent, 0, null);
                    sb.AddLine(EntityBaseHelper.EmbeddedTemplate(repeater, EntityBaseHelper.RenderContent(helper, templateTC, RenderContentMode.Content, repeater), templateTC.Value.ToString()));
                }

                sb.AddLine(repeater.ConstructorScript(JsModule.Lines, "EntityTabRepeater"));
            }

            return sb.ToHtml();
        }

        private static MvcHtmlString InternalTabRepeaterHeader<T>(this HtmlHelper helper, TypeElementContext<T> itemTC, EntityTabRepeater repeater)
        {
            HtmlStringBuilder sb = new HtmlStringBuilder();

            using (sb.Surround(new HtmlTag("li", itemTC.Compose(EntityRepeaterKeys.RepeaterElement)).Let(h => itemTC.Index == 0 ? h.Class("active") : h)
                .Class("sf-repeater-element")))
            {
                using (sb.Surround(new HtmlTag("a")
                    .Attr("href", "#" + itemTC.Compose(EntityBaseKeys.Entity))
                    .Attr("data-toggle", "tab")))
                {
                    sb.Add(new HtmlTag("span").SetInnerText(itemTC.Value.ToString()));

                    sb.AddLine(EntityBaseHelper.WriteIndex(helper, repeater, itemTC));
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
