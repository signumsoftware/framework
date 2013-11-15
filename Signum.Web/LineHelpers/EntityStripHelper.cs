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
            using (sb.Surround(new HtmlTag("div").Id(entityStrip.ControlID).Class("sf-field")))
            using (entityStrip.ValueFirst ? sb.Surround(new HtmlTag("div").Class("sf-value-first")) : null)
            {
                if (!entityStrip.ValueFirst)
                    sb.AddLine(EntityBaseHelper.BaseLineLabel(helper, entityStrip));

                sb.AddLine(helper.HiddenStaticInfo(entityStrip));

                //If it's an embeddedEntity write an empty template with index 0 to be used when creating a new item
                if (entityStrip.ElementType.IsEmbeddedEntity())
                {
                    TypeElementContext<T> templateTC = new TypeElementContext<T>((T)(object)Constructor.Construct(typeof(T)), (TypeContext)entityStrip.Parent, 0);
                    sb.AddLine(EntityBaseHelper.EmbeddedTemplate(entityStrip, EntityBaseHelper.RenderTypeContext(helper, templateTC, RenderMode.Popup, entityStrip)));
                }

                using (sb.Surround(new HtmlTag("ul")
                    .IdName(entityStrip.Compose(EntityStripKeys.ItemsContainer))
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
                                { "class", "sf-value-line sf-entity-autocomplete"},
                                { "autocomplete", "off" }, 
                                { "data-types", new StaticInfo(entityStrip.Type, entityStrip.Implementations, entityStrip.PropertyRoute, entityStrip.ReadOnly).Types.ToString(Navigator.ResolveWebTypeName, ",") }
                            };

                            if (entityStrip.AutocompleteUrl.HasText())
                                htmlAttr.Add("data-url", entityStrip.AutocompleteUrl);

                            sb.AddLine(helper.TextBox(
                                entityStrip.Compose(EntityBaseKeys.ToStr),
                                null,
                                htmlAttr));
                        }

                        sb.AddLine(ListBaseHelper.CreateButton(helper, entityStrip));
                        sb.AddLine(ListBaseHelper.FindButton(helper, entityStrip));
                    }
                }

                if (entityStrip.ShowValidationMessage)
                {
                    sb.AddLine(helper.ValidationMessage(entityStrip.ControlID));
                }

                if (entityStrip.ValueFirst)
                    sb.AddLine(EntityBaseHelper.BaseLineLabel(helper, entityStrip));
            }

            sb.AddLine(new HtmlTag("script").Attr("type", "text/javascript")
                .InnerHtml(new MvcHtmlString("$('#{0}').entityStrip({1})".Formato(entityStrip.ControlID, entityStrip.OptionsJS())))
                .ToHtml());

            return sb.ToHtml();
        }

        private static MvcHtmlString InternalStripElement<T>(this HtmlHelper helper, TypeElementContext<T> itemTC, EntityStrip entityStrip)
        {
            HtmlStringBuilder sb = new HtmlStringBuilder();

            using (sb.Surround(new HtmlTag("li").IdName(itemTC.Compose(EntityStripKeys.StripElement)).Class("sf-strip-element")))
            {
                sb.AddLine(ListBaseHelper.WriteIndex(helper, entityStrip, itemTC, itemTC.Index));
                sb.AddLine(helper.HiddenRuntimeInfo(itemTC));

                if (EntityBaseHelper.EmbeddedOrNew((Modifiable)(object)itemTC.Value))
                    sb.AddLine(EntityBaseHelper.RenderTypeContext(helper, itemTC, RenderMode.PopupInDiv, entityStrip));

                int? id = itemTC.UntypedValue is IdentifiableEntity ? ((IdentifiableEntity)itemTC.UntypedValue).IdOrNull :
                    itemTC.UntypedValue is IdentifiableEntity ? ((Lite<IdentifiableEntity>)itemTC.UntypedValue).IdOrNull : null;
                if (id != null && entityStrip.Navigate && Navigator.IsNavigable(itemTC.Type.CleanType(), entityStrip.PartialViewName, isSearchEntity: false))
                {
                    sb.AddLine(
                        helper.Href(itemTC.Compose(EntityBaseKeys.ToStrLink),
                            itemTC.UntypedValue.ToString(), Navigator.NavigateRoute(itemTC.Type.CleanType(), id), JavascriptMessage.navigate.NiceToString(), "sf-value-line", null));
                }
                else
                {
                    sb.AddLine(
                        helper.Span(itemTC.Compose(EntityBaseKeys.ToStrLink),
                            itemTC.UntypedValue.ToString() ?? " ",
                            "sf-value-line"));
                }


                using (sb.Surround(new HtmlTag("span").Class("sf-button-container")))
                {

                    if (entityStrip.Reorder)
                    {
                        sb.AddLine(
                            helper.Span(itemTC.Compose("btnUp"),
                                JavascriptMessage.moveUp.NiceToString(),
                                "sf-line-button sf-move-up",
                                new Dictionary<string, object> 
                                {  
                                   { "onclick", "{0}.moveUp('{1}');".Formato(entityStrip.ToJS(), itemTC.ControlID) },
                                   { "data-icon",  "ui-icon-triangle-1-" + (entityStrip.Vertical ? "n" : "w") },
                                   { "data-text", false },
                                   { "title", JavascriptMessage.moveUp.NiceToString() }
                                }));

                        sb.AddLine(
                            helper.Span(itemTC.Compose("btnDown"),
                                JavascriptMessage.moveDown.NiceToString(),
                                "sf-line-button sf-move-down",
                                new Dictionary<string, object> 
                                {   
                                   { "onclick", "{0}.moveDown('{1}');".Formato(entityStrip.ToJS(), itemTC.ControlID) },
                                   { "data-icon", "ui-icon-triangle-1-" + (entityStrip.Vertical ? "s" : "e")  },
                                   { "data-text", false },
                                   { "title", JavascriptMessage.moveDown.NiceToString() }
                                }));
                    }

                    if (entityStrip.View)
                        sb.AddLine(
                            helper.Href(itemTC.Compose("btnView"),
                                    EntityControlMessage.View.NiceToString(),
                                    "",
                                    EntityControlMessage.View.NiceToString(),
                                    "sf-line-button sf-view",
                                    new Dictionary<string, object> 
                                {
                                    { "onclick", entityStrip.Removing ?? "{0}.view('{1}');".Formato(entityStrip.ToJS(), itemTC.ControlID) },
                                    { "data-icon",  "ui-icon-circle-arrow-e" },
                                    { "data-text", false } 
                                }));

                    if (entityStrip.Remove)
                        sb.AddLine(
                            helper.Href(itemTC.Compose("btnRemove"),
                                    EntityControlMessage.Remove.NiceToString(),
                                    "",
                                    EntityControlMessage.Remove.NiceToString(),
                                    "sf-line-button sf-remove",
                                    new Dictionary<string, object> 
                                {
                                    { "onclick", entityStrip.Removing ?? "{0}.remove('{1}');".Formato(entityStrip.ToJS(), itemTC.ControlID) },
                                    { "data-icon", "ui-icon-circle-close" }, 
                                    { "data-text", false } 
                                }));
                }
            }

            return sb.ToHtml();
        }

        public static MvcHtmlString EntityStrip<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property)
            where S : Modifiable
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
