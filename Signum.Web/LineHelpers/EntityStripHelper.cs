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
                    .Class("sf-strip").Class(entityStrip.Vertical ? "sf-strip-vertical" : "sf-strip-horizontal")))
                {
                    if (entityStrip.UntypedValue != null)
                    {
                        foreach (var itemTC in TypeContextUtilities.TypeElementContext((TypeContext<MList<T>>)entityStrip.Parent))
                            sb.Add(InternalStripElement(helper, itemTC, entityStrip));
                    }

                    using (sb.Surround(new HtmlTag("li").Class("sf-strip-input input-group")))
                    {
                        if (entityStrip.Autocomplete)
                        {
                            var htmlAttr = new Dictionary<string, object>
                            {
                                { "class", "sf-entity-autocomplete"},
                                { "autocomplete", "off" }, 
                            };

                            sb.AddLine(helper.TextBox(
                                entityStrip.Compose(EntityBaseKeys.ToStr),
                                null,
                                htmlAttr));
                        }

                        using (sb.Surround(new HtmlTag("span", entityStrip.Compose("shownButton"))))
                        {
                            sb.AddLine(EntityButtonHelper.Create(helper, entityStrip, btn: false));
                            sb.AddLine(EntityButtonHelper.Find(helper, entityStrip, btn: false));
                        }
                    }
                }

                if (entityStrip.ElementType.IsEmbeddedEntity() && entityStrip.Create)
                {
                    TypeElementContext<T> templateTC = new TypeElementContext<T>((T)(object)helper.ViewContext.Controller.Construct(typeof(T)), (TypeContext)entityStrip.Parent, 0, null);
                    sb.AddLine(EntityBaseHelper.EmbeddedTemplate(entityStrip, EntityBaseHelper.RenderPopup(helper, templateTC, RenderPopupMode.Popup, entityStrip, isTemplate: true), null));
                }

                sb.AddLine(entityStrip.ConstructorScript(JsModule.Lines, "EntityStrip"));
            }

            return helper.FormGroup(entityStrip, entityStrip.Prefix, entityStrip.LabelText, sb.ToHtml());
        }

        private static MvcHtmlString InternalStripElement<T>(this HtmlHelper helper, TypeElementContext<T> itemTC, EntityStrip entityStrip)
        {
            HtmlStringBuilder sb = new HtmlStringBuilder();

            using (sb.Surround(new HtmlTag("li").IdName(itemTC.Compose(EntityStripKeys.StripElement)).Class("sf-strip-element input-group")))
            {
                var lite = (itemTC.UntypedValue as Lite<IIdentifiable>) ?? (itemTC.UntypedValue as IIdentifiable).Try(i => i.ToLite(i.IsNew));

                if (lite != null && !lite.IsNew && entityStrip.Navigate)
                {
                    sb.AddLine(
                        helper.Href(itemTC.Compose(EntityBaseKeys.Link),
                        lite.ToString(), lite.IdOrNull == null ? null : Navigator.NavigateRoute(lite),
                            JavascriptMessage.navigate.NiceToString(), "sf-entitStrip-link", null));
                }
                else
                {
                    sb.AddLine(
                        helper.Span(itemTC.Compose(EntityBaseKeys.Link),
                            itemTC.UntypedValue.ToString() ?? " ", "sf-entitStrip-link"));
                }

                sb.AddLine(EntityBaseHelper.WriteIndex(helper, itemTC));
                sb.AddLine(helper.HiddenRuntimeInfo(itemTC));

                if (EntityBaseHelper.EmbeddedOrNew((Modifiable)(object)itemTC.Value))
                    sb.AddLine(EntityBaseHelper.RenderPopup(helper, itemTC, RenderPopupMode.PopupInDiv, entityStrip));

                using (sb.Surround(new HtmlTag("span")))
                {
                    if (entityStrip.Reorder)
                    {
                        sb.AddLine(EntityButtonHelper.MoveUpItem(helper, itemTC, entityStrip, btn: false, elementType: "a", isVertical: entityStrip.Vertical));
                        sb.AddLine(EntityButtonHelper.MoveDownItem(helper, itemTC, entityStrip, btn: false, elementType: "a", isVertical: entityStrip.Vertical));
                    }

                    if (entityStrip.View)
                        sb.AddLine(EntityButtonHelper.ViewItem(helper, itemTC, entityStrip, btn: false));

                    if (entityStrip.Remove)
                        sb.AddLine(EntityButtonHelper.RemoveItem(helper, itemTC, entityStrip, btn: false));
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

            var vo = tc.ViewOverrides;

            if (vo != null && !vo.IsVisible(context.PropertyRoute))
                return vo.OnSurroundLine(context.PropertyRoute, helper, tc, null);

            EntityStrip es = new EntityStrip(context.Type, context.UntypedValue, context, null, context.PropertyRoute);

            EntityBaseHelper.ConfigureEntityBase(es, typeof(S).CleanType());

            Common.FireCommonTasks(es);

            if (settingsModifier != null)
                settingsModifier(es);

            var result = helper.InternalEntityStrip<S>(es);

            if (vo == null)
                return result;

            return vo.OnSurroundLine(es.PropertyRoute, helper, tc, result);
        }
    }
}
