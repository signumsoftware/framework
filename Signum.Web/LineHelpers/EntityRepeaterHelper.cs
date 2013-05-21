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

            using (sb.Surround(new HtmlTag("fieldset").Id(entityRepeater.ControlID).Class("sf-repeater-field")))
            {
                using (sb.Surround(new HtmlTag("legend")))
                {
                    sb.AddLine(EntityBaseHelper.BaseLineLabel(helper, entityRepeater));
                    
                    sb.AddLine(ListBaseHelper.CreateButton(helper, entityRepeater, new Dictionary<string, object> { { "title", entityRepeater.AddElementLinkText } }));
                    sb.AddLine(ListBaseHelper.FindButton(helper, entityRepeater));
                }

                sb.AddLine(helper.HiddenStaticInfo(entityRepeater));
                sb.AddLine(helper.Hidden(entityRepeater.Compose(EntityListBaseKeys.ListPresent), ""));

                //If it's an embeddedEntity write an empty template with index 0 to be used when creating a new item
                if (entityRepeater.ElementType.IsEmbeddedEntity())
                {
                    TypeElementContext<T> templateTC = new TypeElementContext<T>((T)(object)Constructor.Construct(typeof(T)), (TypeContext)entityRepeater.Parent, 0);
                    sb.AddLine(EntityBaseHelper.EmbeddedTemplate(entityRepeater, EntityBaseHelper.RenderTypeContext(helper, templateTC, RenderMode.Content, entityRepeater)));
                }
                
                using (sb.Surround(new HtmlTag("div").IdName(entityRepeater.Compose(EntityRepeaterKeys.ItemsContainer))))
                {
                    if (entityRepeater.UntypedValue != null)
                    {
                        foreach (var itemTC in TypeContextUtilities.TypeElementContext((TypeContext<MList<T>>)entityRepeater.Parent))
                            sb.Add(InternalRepeaterElement(helper, itemTC, entityRepeater));
                    }
                }
            }

            sb.AddLine(new HtmlTag("script").Attr("type", "text/javascript")
                .InnerHtml(new MvcHtmlString("$('#{0}').entityRepeater({1})".Formato(entityRepeater.ControlID, entityRepeater.OptionsJS())))
                .ToHtml());

            return sb.ToHtml();
        }

        private static MvcHtmlString InternalRepeaterElement<T>(this HtmlHelper helper, TypeElementContext<T> itemTC, EntityRepeater entityRepeater)
        {
            HtmlStringBuilder sb = new HtmlStringBuilder();

            using (sb.Surround(new HtmlTag("fieldset").IdName(itemTC.Compose(EntityRepeaterKeys.RepeaterElement)).Class("sf-repeater-element")))
            {
                using (sb.Surround(new HtmlTag("legend")))
                { 
                    if (entityRepeater.Remove)
                        sb.AddLine(
                            helper.Href(itemTC.Compose("btnRemove"),
                                    entityRepeater.RemoveElementLinkText,
                                    "",
                                    entityRepeater.RemoveElementLinkText,
                                    "sf-line-button sf-remove",
                                    new Dictionary<string, object> 
                                    {
                                        { "onclick", "{0}.remove('{1}');".Formato(entityRepeater.ToJS(), itemTC.ControlID) },
                                        { "data-icon", "ui-icon-circle-close" }, 
                                        { "data-text", false } 
                                    }));

                    if (entityRepeater.Reorder)
                    {
                        sb.AddLine(
                            helper.Span(itemTC.Compose("btnUp"),
                                JavascriptMessage.entityRepeater_moveUp.NiceToString(),
                                "sf-line-button sf-move-up",
                                new Dictionary<string, object> 
                                {  
                                   { "onclick", "{0}.moveUp('{1}');".Formato(entityRepeater.ToJS(), itemTC.ControlID) },
                                   { "data-icon", "ui-icon-triangle-1-n" },
                                   { "data-text", false },
                                   { "title", JavascriptMessage.entityRepeater_moveUp.NiceToString() }
                                }));

                        sb.AddLine(
                            helper.Span(itemTC.Compose("btnDown"),
                                JavascriptMessage.entityRepeater_moveDown.NiceToString(),
                                "sf-line-button sf-move-down",
                                new Dictionary<string, object> 
                                {   
                                   { "onclick", "{0}.moveDown('{1}');".Formato(entityRepeater.ToJS(), itemTC.ControlID) },
                                   { "data-icon", "ui-icon-triangle-1-s" },
                                   { "data-text", false },
                                   { "title", JavascriptMessage.entityRepeater_moveDown.NiceToString() }
                                }));
                    }
                }

                sb.AddLine(ListBaseHelper.WriteIndex(helper, entityRepeater, itemTC, itemTC.Index));
                sb.AddLine(helper.HiddenRuntimeInfo(itemTC));

                sb.AddLine(EntityBaseHelper.RenderTypeContext(helper, itemTC, RenderMode.ContentInVisibleDiv, entityRepeater));
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
