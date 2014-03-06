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

            using (sb.Surround(new HtmlTag("fieldset").Id(entityTabRepeater.Prefix).Class("sf-repeater-field SF-control-container")))
            {
                using (sb.Surround(new HtmlTag("legend")))
                {
                    sb.AddLine(EntityBaseHelper.BaseLineLabel(helper, entityTabRepeater));

                    sb.AddLine(EntityBaseHelper.CreateButton(helper, entityTabRepeater, hidden: false));
                    sb.AddLine(EntityBaseHelper.FindButton(helper, entityTabRepeater, hidden: false));
                }

                sb.AddLine(helper.Hidden(entityTabRepeater.Compose(EntityListBaseKeys.ListPresent), ""));

                //If it's an embeddedEntity write an empty template with index 0 to be used when creating a new item
                if (entityTabRepeater.ElementType.IsEmbeddedEntity())
                {
                    TypeElementContext<T> templateTC = new TypeElementContext<T>((T)(object)Constructor.Construct(typeof(T)), (TypeContext)entityTabRepeater.Parent, 0);
                    sb.AddLine(EntityBaseHelper.EmbeddedTemplate(entityTabRepeater, EntityBaseHelper.RenderContent(helper, templateTC, RenderContentMode.Content, entityTabRepeater), templateTC.Value.ToString()));
                }

                using (sb.Surround(new HtmlTag("div").Id(entityTabRepeater.Compose(EntityRepeaterKeys.TabsContainer))))
                {
                    using (sb.Surround(new HtmlTag("ul").Id(entityTabRepeater.Compose(EntityRepeaterKeys.ItemsContainer))))
                    {
                        if (entityTabRepeater.UntypedValue != null)
                        {
                            foreach (var itemTC in TypeContextUtilities.TypeElementContext((TypeContext<MList<T>>)entityTabRepeater.Parent))
                                sb.Add(InternalTabRepeaterHeader(helper, itemTC, entityTabRepeater));
                        }
                    }

                    if (entityTabRepeater.UntypedValue != null)
                    {
                        foreach (var itemTC in TypeContextUtilities.TypeElementContext((TypeContext<MList<T>>)entityTabRepeater.Parent))
                            sb.Add(EntityBaseHelper.RenderContent(helper, itemTC, RenderContentMode.ContentInVisibleDiv, entityTabRepeater));
                    }
                }
            }

            sb.AddLine(entityTabRepeater.ConstructorScript(JsFunction.LinesModule, "EntityTabRepeater"));

            return sb.ToHtml();
        }

        private static MvcHtmlString InternalTabRepeaterHeader<T>(this HtmlHelper helper, TypeElementContext<T> itemTC, EntityTabRepeater entityTabRepeater)
        {
            HtmlStringBuilder sb = new HtmlStringBuilder();

            using (sb.Surround(new HtmlTag("li").Id(itemTC.Compose(EntityRepeaterKeys.RepeaterElement)).Class("sf-repeater-element")))
            {
                sb.AddLine(new HtmlTag("a").Attr("href", "#" + itemTC.Compose(EntityBaseKeys.Entity)).SetInnerText(itemTC.Value.ToString()).ToHtml());

                if (entityTabRepeater.Remove)
                    sb.AddLine(EntityListBaseHelper.RemoveButtonItem(helper, itemTC, entityTabRepeater));

                if (entityTabRepeater.Reorder)
                {
                    sb.AddLine(EntityListBaseHelper.MoveUpButtonItem(helper, itemTC, entityTabRepeater, true));
                    sb.AddLine(EntityListBaseHelper.MoveDownButtonItem(helper, itemTC, entityTabRepeater, true));
                }
                sb.AddLine(EntityListBaseHelper.WriteIndex(helper, entityTabRepeater, itemTC, itemTC.Index));
                sb.AddLine(helper.HiddenRuntimeInfo(itemTC));
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
