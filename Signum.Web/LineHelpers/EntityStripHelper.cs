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
    public static class EntityStripHelper
    {
        private static MvcHtmlString InternalEntityStrip<T>(this HtmlHelper helper, EntityStrip entityStrip)
        {
            if (!entityStrip.Visible || entityStrip.HideIfNull && entityStrip.UntypedValue == null)
                return MvcHtmlString.Empty;

            
            HtmlStringBuilder sb = new HtmlStringBuilder();
            using (sb.Surround(new HtmlTag("div", entityStrip.Prefix).Class("SF-entity-strip SF-control-container")))
            {
                sb.AddLine(helper.Hidden(entityStrip.Compose(EntityListBaseKeys.ListPresent), ""));

                using (sb.Surround(new HtmlTag("div", entityStrip.Compose("hidden")).Class("hide")))
                {
                }

                using (sb.Surround(new HtmlTag("ul", entityStrip.Compose(EntityStripKeys.ItemsContainer))
                    .Class("sf-strip").Class(entityStrip.Vertical ? "sf-strip-vertical" : null)))
                {
                    if (entityStrip.UntypedValue != null)
                    {
                        foreach (var itemTC in TypeContextUtilities.TypeElementContext((TypeContext<MList<T>>)entityStrip.Parent))
                            sb.Add(InternalStripElement(helper, itemTC, entityStrip));
                    }

                    using (sb.Surround(new HtmlTag("li").Class("sf-strip-input")))
                    {
                        if (entityStrip.Autocomplete)
                        {
                            var htmlAttr = new Dictionary<string, object>
                            {
                                { "class", "sf-entity-autocomplete"},
                                { "autocomplete", "off" }, 
                            };

                            if (entityStrip.AutocompleteUrl.HasText())
                                htmlAttr.Add("data-url", entityStrip.AutocompleteUrl);

                            sb.AddLine(helper.TextBox(
                                entityStrip.Compose(EntityBaseKeys.ToStr),
                                null,
                                htmlAttr));
                        }

                        using (sb.Surround(new HtmlTag("span", entityStrip.Compose("shownButton")).Class(entityStrip.Vertical ? "pull-right" : null)))
                        {
                            sb.AddLine(EntityListBaseHelper.CreateSpan(helper, entityStrip));
                            sb.AddLine(EntityListBaseHelper.FindSpan(helper, entityStrip));
                        }
                    }
                }

                if (entityStrip.ElementType.IsEmbeddedEntity())
                {
                    TypeElementContext<T> templateTC = new TypeElementContext<T>((T)(object)Constructor.Construct(typeof(T)), (TypeContext)entityStrip.Parent, 0);
                    sb.AddLine(EntityBaseHelper.EmbeddedTemplate(entityStrip, EntityBaseHelper.RenderPopup(helper, templateTC, RenderPopupMode.Popup, entityStrip, isTemplate: true), null));
                }

                sb.AddLine(entityStrip.ConstructorScript(JsFunction.LinesModule, "EntityStrip"));
            }

            return helper.FormGroup(entityStrip, entityStrip.Prefix, entityStrip.LabelText, sb.ToHtml());
        }

        private static MvcHtmlString InternalStripElement<T>(this HtmlHelper helper, TypeElementContext<T> itemTC, EntityStrip entityStrip)
        {
            HtmlStringBuilder sb = new HtmlStringBuilder();

            using (sb.Surround(new HtmlTag("li").IdName(itemTC.Compose(EntityStripKeys.StripElement)).Class("sf-strip-element")))
            {
                var id = (itemTC.UntypedValue as IIdentifiable).TryCS(i => i.IdOrNull) ??
                        (itemTC.UntypedValue as Lite<IIdentifiable>).TryCS(l => l.IdOrNull);

                if (id != null && entityStrip.Navigate)
                {
                    sb.AddLine(
                        helper.Href(itemTC.Compose(EntityBaseKeys.Link),
                            itemTC.UntypedValue.ToString(), Navigator.NavigateRoute(itemTC.Type.CleanType(), id),
                            JavascriptMessage.navigate.NiceToString(), "sf-entitStrip-link", null));
                }
                else
                {
                    sb.AddLine(
                        helper.Span(itemTC.Compose(EntityBaseKeys.Link),
                            itemTC.UntypedValue.ToString() ?? " ", "sf-entitStrip-link"));
                }

                sb.AddLine(EntityListBaseHelper.WriteIndex(helper, entityStrip, itemTC, itemTC.Index));
                sb.AddLine(helper.HiddenRuntimeInfo(itemTC));

                if (EntityBaseHelper.EmbeddedOrNew((Modifiable)(object)itemTC.Value))
                    sb.AddLine(EntityBaseHelper.RenderPopup(helper, itemTC, RenderPopupMode.PopupInDiv, entityStrip));


                using (sb.Surround(new HtmlTag("span").Class(entityStrip.Vertical ? "pull-right" : null)))
                {
                    if (entityStrip.Reorder)
                    {
                        sb.AddLine(EntityListBaseHelper.MoveUpSpanItem(helper, itemTC, entityStrip, "a", entityStrip.Vertical));
                        sb.AddLine(EntityListBaseHelper.MoveDownSpanItem(helper, itemTC, entityStrip, "a", entityStrip.Vertical));
                    }

                    if (entityStrip.View)
                        sb.AddLine(EntityListBaseHelper.ViewSpanItem(helper, itemTC, entityStrip, "a"));

                    if (entityStrip.Remove)
                        sb.AddLine(EntityListBaseHelper.RemoveSpanItem(helper, itemTC, entityStrip, "a"));
                }
            }

            return sb.ToHtml();
        }

        public static MvcHtmlString EntityStrip<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property)
        {
            return helper.EntityStrip(tc, property, null);
        }

        public static MvcHtmlString EntityStrip<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property, Action<EntityStrip> settingsModifier)
        {
            TypeContext<MList<S>> context = Common.WalkExpression(tc, property);

            EntityStrip es = new EntityStrip(context.Type, context.UntypedValue, context, null, context.PropertyRoute);

            EntityBaseHelper.ConfigureEntityBase(es, typeof(S).CleanType());

            Common.FireCommonTasks(es);

            if (settingsModifier != null)
                settingsModifier(es);

            var result = helper.InternalEntityStrip<S>(es);

            var vo = es.ViewOverrides;
            if (vo == null)
                return result;

            return vo.OnSurroundLine(es.PropertyRoute, helper, tc, result);
        }
    }
}
