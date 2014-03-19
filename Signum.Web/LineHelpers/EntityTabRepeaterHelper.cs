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
        private static MvcHtmlString InternalEntityRepeater<T>(this HtmlHelper helper, EntityTabRepeater entityTabRepeater)
        {
            if (!entityTabRepeater.Visible || entityTabRepeater.HideIfNull && entityTabRepeater.UntypedValue == null)
                return MvcHtmlString.Empty;

            HtmlStringBuilder sb = new HtmlStringBuilder();

            using (sb.Surround(new HtmlTag("fieldset").Id(entityTabRepeater.Prefix).Class("sf-tab-repeater-field SF-control-container")))
            {
                using (sb.Surround(new HtmlTag("legend")))
                using (sb.Surround(new HtmlTag("div", entityTabRepeater.Compose("header"))))
                {
                    sb.AddLine(new HtmlTag("span").SetInnerText(entityTabRepeater.LabelText).ToHtml());

                    using (sb.Surround(new HtmlTag("span", entityTabRepeater.Compose("shownButton")).Class("pull-right")))
                    {
                        sb.AddLine(EntityListBaseHelper.CreateSpan(helper, entityTabRepeater));
                        sb.AddLine(EntityListBaseHelper.FindSpan(helper, entityTabRepeater));
                    }
                }

                sb.AddLine(helper.Hidden(entityTabRepeater.Compose(EntityListBaseKeys.ListPresent), ""));

                using (sb.Surround(new HtmlTag("div").Id(entityTabRepeater.Compose(EntityRepeaterKeys.TabsContainer))))
                {
                    using (sb.Surround(new HtmlTag("ul").Class("nav nav-tabs").Id(entityTabRepeater.Compose(EntityRepeaterKeys.ItemsContainer))))
                    {
                        if (entityTabRepeater.UntypedValue != null)
                        {
                            foreach (var itemTC in TypeContextUtilities.TypeElementContext((TypeContext<MList<T>>)entityTabRepeater.Parent))
                                sb.Add(InternalTabRepeaterHeader(helper, itemTC, entityTabRepeater));
                        }
                    }

                    using (sb.Surround(new HtmlTag("div").Class("tab-content").Id(entityTabRepeater.Compose(EntityRepeaterKeys.ItemsContainer))))
                        if (entityTabRepeater.UntypedValue != null)
                        {
                            foreach (var itemTC in TypeContextUtilities.TypeElementContext((TypeContext<MList<T>>)entityTabRepeater.Parent))
                                using (sb.Surround(new HtmlTag("div", itemTC.Compose(EntityBaseKeys.Entity)).Class("tab-pane")
                                    .Let(h => itemTC.Index == 0 ? h.Class("active") : h)))
                                    sb.Add(EntityBaseHelper.RenderContent(helper, itemTC, RenderContentMode.Content, entityTabRepeater));
                        }
                }

                //If it's an embeddedEntity write an empty template with index 0 to be used when creating a new item
                if (entityTabRepeater.ElementType.IsEmbeddedEntity())
                {
                    TypeElementContext<T> templateTC = new TypeElementContext<T>((T)(object)Constructor.Construct(typeof(T)), (TypeContext)entityTabRepeater.Parent, 0);
                    sb.AddLine(EntityBaseHelper.EmbeddedTemplate(entityTabRepeater, EntityBaseHelper.RenderContent(helper, templateTC, RenderContentMode.Content, entityTabRepeater), templateTC.Value.ToString()));
                }

                sb.AddLine(entityTabRepeater.ConstructorScript(JsFunction.LinesModule, "EntityTabRepeater"));
            }

            return sb.ToHtml();
        }

        private static MvcHtmlString InternalTabRepeaterHeader<T>(this HtmlHelper helper, TypeElementContext<T> itemTC, EntityTabRepeater entityTabRepeater)
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

                    sb.AddLine(EntityListBaseHelper.WriteIndex(helper, entityTabRepeater, itemTC, itemTC.Index));
                    sb.AddLine(helper.HiddenRuntimeInfo(itemTC));

                    if (entityTabRepeater.Reorder)
                    {
                        sb.AddLine(EntityListBaseHelper.MoveUpSpanItem(helper, itemTC, entityTabRepeater, "span", false));
                        sb.AddLine(EntityListBaseHelper.MoveDownSpanItem(helper, itemTC, entityTabRepeater, "span", false));
                    }

                    if (entityTabRepeater.Remove)
                        sb.AddLine(EntityListBaseHelper.RemoveSpanItem(helper, itemTC, entityTabRepeater, "span"));
                }

            }

            return sb.ToHtml();
        }

        public static MvcHtmlString EntityTabRepeater<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property)
            where S : Modifiable
        {
            return helper.EntityRepeater(tc, property, null);
        }

        public static MvcHtmlString EntityTabRepeater<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property, Action<EntityTabRepeater> settingsModifier)
        {
            TypeContext<MList<S>> context = Common.WalkExpression(tc, property);

            EntityTabRepeater el = new EntityTabRepeater(context.Type, context.UntypedValue, context, null, context.PropertyRoute);

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
