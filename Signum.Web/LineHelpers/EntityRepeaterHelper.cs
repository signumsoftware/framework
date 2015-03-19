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
    public static class EntityRepeaterHelper
    {
        private static MvcHtmlString InternalEntityRepeater<T>(this HtmlHelper helper, EntityRepeater repeater)
        {
            if (!repeater.Visible || repeater.HideIfNull && repeater.UntypedValue == null)
                return MvcHtmlString.Empty;

            HtmlStringBuilder sb = new HtmlStringBuilder();
            using (sb.SurroundLine(new HtmlTag("fieldset", repeater.Prefix).Class("SF-repeater-field SF-control-container SF-avoid-child-errors")))
            {
                sb.AddLine(helper.Hidden(repeater.Compose(EntityListBaseKeys.ListPresent), ""));

                using (sb.SurroundLine(new HtmlTag("div", repeater.Compose("hidden")).Class("hide")))
                {
                }

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

                using (sb.SurroundLine(new HtmlTag("div").Id(repeater.Compose(EntityRepeaterKeys.ItemsContainer))))
                {
                    if (repeater.UntypedValue != null)
                    {
                        foreach (var itemTC in TypeContextUtilities.TypeElementContext((TypeContext<MList<T>>)repeater.Parent))
                            sb.Add(InternalRepeaterElement(helper, itemTC, repeater));
                    }
                }


                if (repeater.ElementType.IsEmbeddedEntity() && repeater.Create)
                {
                    T embedded = (T)(object)new ConstructorContext(helper.ViewContext.Controller).ConstructUntyped(typeof(T));
                    TypeElementContext<T> templateTC = new TypeElementContext<T>(embedded, (TypeContext)repeater.Parent, 0, null);
                    sb.AddLine(EntityBaseHelper.EmbeddedTemplate(repeater, EntityBaseHelper.RenderContent(helper, templateTC, RenderContentMode.Content, repeater), null));
                }

                sb.AddLine(repeater.ConstructorScript(JsModule.Lines, "EntityRepeater"));
            }

            return sb.ToHtml();
        }

        private static MvcHtmlString InternalRepeaterElement<T>(this HtmlHelper helper, TypeElementContext<T> itemTC, EntityRepeater repeater)
        {
            HtmlStringBuilder sb = new HtmlStringBuilder();

            if (repeater.IsVisible == null || repeater.IsVisible(itemTC))
            {
                using (sb.SurroundLine(new HtmlTag("fieldset", itemTC.Compose(EntityRepeaterKeys.RepeaterElement)).Class("sf-repeater-element")))
                {
                    using (sb.SurroundLine(new HtmlTag("legend")))
                    {
                        if (repeater.Remove)
                            sb.AddLine(EntityButtonHelper.RemoveItem(helper, itemTC, repeater, btn: false, elementType: "a"));

                        if (repeater.Move)
                        {
                            sb.AddLine(EntityButtonHelper.MoveUpItem(helper, itemTC, repeater, btn: false, elementType: "a", isVertical: true));
                            sb.AddLine(EntityButtonHelper.MoveDownItem(helper, itemTC, repeater, btn: false, elementType: "a", isVertical: true));
                        }
                    }

                    sb.AddLine(EntityBaseHelper.WriteIndex(helper, itemTC));
                    sb.AddLine(helper.HiddenRuntimeInfo(itemTC));

                    sb.AddLine(EntityBaseHelper.RenderContent(helper, itemTC, RenderContentMode.ContentInVisibleDiv, repeater));
                }
            }
            else
            {
                using (sb.SurroundLine(new HtmlTag("fieldset", itemTC.Compose(EntityRepeaterKeys.RepeaterElement)).Class("sf-repeater-element hidden")))
                {
                    sb.AddLine(EntityBaseHelper.WriteIndex(helper, itemTC));
                    sb.AddLine(helper.HiddenRuntimeInfo(itemTC));
                }
            }

            return sb.ToHtml();
        }

        public static MvcHtmlString EntityRepeater<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property)
            where S : Modifiable
        {
            return helper.EntityRepeater(tc, property, null);
        }

        public static MvcHtmlString EntityRepeater<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property, Action<EntityRepeater> settingsModifier)
        {
            TypeContext<MList<S>> context = Common.WalkExpression(tc, property);

            var vo = tc.ViewOverrides;

            if (vo != null && !vo.IsVisible(context.PropertyRoute))
                return vo.OnSurroundLine(context.PropertyRoute, helper, tc, null);

            EntityRepeater repeater = new EntityRepeater(context.Type, context.UntypedValue, context, null, context.PropertyRoute);

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
