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
    public static class EntityRepeaterHelper
    {
        private static MvcHtmlString InternalEntityRepeater<T>(this HtmlHelper helper, EntityRepeater entityRepeater)
        {
            if (!entityRepeater.Visible || entityRepeater.HideIfNull && entityRepeater.UntypedValue == null)
                return MvcHtmlString.Empty;

            HtmlStringBuilder sb = new HtmlStringBuilder();
            using (sb.Surround(new HtmlTag("fieldset", entityRepeater.Prefix).Class("SF-repeater-field SF-control-container")))
            {
                sb.AddLine(helper.Hidden(entityRepeater.Compose(EntityListBaseKeys.ListPresent), ""));

                using (sb.Surround(new HtmlTag("div", entityRepeater.Compose("hidden")).Class("hide")))
                {
                }

                using (sb.Surround(new HtmlTag("legend")))
                using (sb.Surround(new HtmlTag("div", entityRepeater.Compose("header"))))
                {
                    sb.AddLine(new HtmlTag("span").SetInnerText(entityRepeater.LabelText).ToHtml());

                    using (sb.Surround(new HtmlTag("span", entityRepeater.Compose("shownButton")).Class("pull-right")))
                    {
                        sb.AddLine(EntityButtonHelper.Create(helper, entityRepeater, btn: false));
                        sb.AddLine(EntityButtonHelper.Find(helper, entityRepeater, btn: false));
                    }
                }

                using (sb.Surround(new HtmlTag("div").Id(entityRepeater.Compose(EntityRepeaterKeys.ItemsContainer))))
                {
                    if (entityRepeater.UntypedValue != null)
                    {
                        foreach (var itemTC in TypeContextUtilities.TypeElementContext((TypeContext<MList<T>>)entityRepeater.Parent))
                            sb.Add(InternalRepeaterElement(helper, itemTC, entityRepeater));
                    }
                }


                if (entityRepeater.ElementType.IsEmbeddedEntity())
                {
                    TypeElementContext<T> templateTC = new TypeElementContext<T>((T)(object)Constructor.Construct(typeof(T)), (TypeContext)entityRepeater.Parent, 0);
                    sb.AddLine(EntityBaseHelper.EmbeddedTemplate(entityRepeater, EntityBaseHelper.RenderContent(helper, templateTC, RenderContentMode.Content, entityRepeater), null));
                }

                sb.AddLine(entityRepeater.ConstructorScript(JsFunction.LinesModule, "EntityRepeater"));
            }

            return sb.ToHtml();
        }

        private static MvcHtmlString InternalRepeaterElement<T>(this HtmlHelper helper, TypeElementContext<T> itemTC, EntityRepeater entityRepeater)
        {
            HtmlStringBuilder sb = new HtmlStringBuilder();

            using (sb.Surround(new HtmlTag("fieldset", itemTC.Compose(EntityRepeaterKeys.RepeaterElement)).Class("sf-repeater-element")))
            {
                using (sb.Surround(new HtmlTag("legend")))
                {
                    if (entityRepeater.Remove)
                        sb.AddLine(EntityButtonHelper.RemoveItem(helper, itemTC, entityRepeater, btn: false, elementType: "a"));

                    if (entityRepeater.Reorder)
                    {
                        sb.AddLine(EntityButtonHelper.MoveUpItem(helper, itemTC, entityRepeater, btn: false, elementType: "a", isVertical: true));
                        sb.AddLine(EntityButtonHelper.MoveDownItem(helper, itemTC, entityRepeater, btn: false, elementType: "a", isVertical: true));
                    }
                }

                sb.AddLine(EntityBaseHelper.WriteIndex(helper, entityRepeater, itemTC, itemTC.Index));
                sb.AddLine(helper.HiddenRuntimeInfo(itemTC));

                sb.AddLine(EntityBaseHelper.RenderContent(helper, itemTC, RenderContentMode.ContentInVisibleDiv, entityRepeater));
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

            EntityRepeater el = new EntityRepeater(context.Type, context.UntypedValue, context, null, context.PropertyRoute);

            EntityBaseHelper.ConfigureEntityBase(el, typeof(S).CleanType());

            Common.FireCommonTasks(el);

            if (settingsModifier != null)
                settingsModifier(el);

            var result = helper.InternalEntityRepeater<S>(el);

            var vo = el.ViewOverrides;
            if (vo == null)
                return result;

            return vo.OnSurroundLine(el.PropertyRoute, helper, tc, result);
        }
    }
}
