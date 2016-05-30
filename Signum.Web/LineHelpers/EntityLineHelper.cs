using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Web.Mvc.Html;
using Signum.Entities;
using System.Reflection;
using Signum.Entities.Reflection;
using System.Configuration;
using Signum.Engine;
using Signum.Utilities.Reflection;
using Signum.Web.Controllers;

namespace Signum.Web
{
    public static class EntityLineHelper
    {
        internal static MvcHtmlString InternalEntityLine(this HtmlHelper helper, EntityLine entityLine)
        {
            if (!entityLine.Visible || (entityLine.HideIfNull && entityLine.UntypedValue == null))
                return MvcHtmlString.Empty;

            HtmlStringBuilder sb = new HtmlStringBuilder();
            using (sb.SurroundLine(new HtmlTag("div", entityLine.Prefix).Class("SF-entity-line SF-control-container")))
            {
                 sb.AddLine(helper.HiddenRuntimeInfo(entityLine));

                 using (sb.SurroundLine(new HtmlTag("div", entityLine.Compose("hidden")).Class("hide")))
                 {
                     if (entityLine.UntypedValue != null)
                     {
                         sb.AddLine(AutocompleteTextBox(helper, entityLine));
                         sb.AddLine(EntityButtonHelper.Create(helper, entityLine, btn: true));
                         sb.AddLine(EntityButtonHelper.Find(helper, entityLine, btn: true));
                     }
                     else
                     {
                         sb.AddLine(LinkOrSpan(helper, entityLine));
                         sb.AddLine(EntityButtonHelper.View(helper, entityLine, btn: true));
                         sb.AddLine(EntityButtonHelper.Remove(helper, entityLine, btn: true));
                     }
                 }

                 using (sb.SurroundLine(new HtmlTag("div", entityLine.Compose("inputGroup")).Class("input-group")))
                 {
                     if (entityLine.UntypedValue == null)
                         sb.AddLine(AutocompleteTextBox(helper, entityLine));
                     else
                         sb.AddLine(LinkOrSpan(helper, entityLine));

                     using (sb.SurroundLine(new HtmlTag("span", entityLine.Compose("shownButton")).Class("input-group-btn")))
                     {
                         if (entityLine.UntypedValue == null)
                         {
                             sb.AddLine(EntityButtonHelper.Create(helper, entityLine, btn: true));
                             sb.AddLine(EntityButtonHelper.Find(helper, entityLine, btn: true));
                         }
                         else
                         {
                             sb.AddLine(EntityButtonHelper.View(helper, entityLine, btn: true));
                             sb.AddLine(EntityButtonHelper.Remove(helper, entityLine, btn: true));
                         }
                     }
                 }

                 if (entityLine.Type.IsEmbeddedEntity() && entityLine.Create)
                 {
                     EmbeddedEntity embedded = (EmbeddedEntity)new ConstructorContext(helper.ViewContext.Controller).ConstructUntyped(entityLine.Type.CleanType());
                     TypeContext templateTC = ((TypeContext)entityLine.Parent).Clone(embedded);
                     sb.AddLine(EntityBaseHelper.EmbeddedTemplate(entityLine, EntityBaseHelper.RenderPopup(helper, templateTC, RenderPopupMode.Popup, entityLine, isTemplate: true), null));
                 }

                if (EntityBaseHelper.EmbeddedOrNew((Modifiable)entityLine.UntypedValue))
                    sb.AddLine(EntityBaseHelper.RenderPopup(helper, (TypeContext)entityLine.Parent, RenderPopupMode.PopupInDiv, entityLine));


                sb.AddLine(entityLine.ConstructorScript(JsModule.Lines, "EntityLine"));
            }

            return helper.FormGroup(entityLine, entityLine.Prefix, entityLine.LabelHtml ?? entityLine.LabelText.FormatHtml(), sb.ToHtml());
        }

        private static MvcHtmlString LinkOrSpan(HtmlHelper helper, EntityLine entityLine)
        {
            MvcHtmlString result;
            if (entityLine.Navigate || entityLine.View)
            {
                var lite = (entityLine.UntypedValue as Lite<IEntity>) ??
                           (entityLine.UntypedValue as IEntity)?.Let(i => i.ToLite(i.IsNew));

                var dic = new Dictionary<string, object>
                {
                    { "onclick", entityLine.SFControlThen("view_click(event)") }
                };

                result = helper.Href(entityLine.Compose(EntityBaseKeys.Link),
                        entityLine.UntypedValue?.ToString(),
                        "#",
                        JavascriptMessage.navigate.NiceToString(), entityLine.ReadOnly ? null : "form-control  btn-default sf-entity-line-entity",
                        dic);
            }
            else
            {
                result = helper.Span(entityLine.Compose(EntityBaseKeys.Link),
                        entityLine.UntypedValue?.ToString() ?? " ",
                        entityLine.ReadOnly ? null : "form-control btn-default sf-entity-line-entity");
            }

            if (entityLine.ReadOnly)
                return new HtmlTag("p").Class("form-control-static").InnerHtml(result);

            return result;
        }

        private static MvcHtmlString AutocompleteTextBox(HtmlHelper helper, EntityLine entityLine)
        {
            if (!entityLine.Autocomplete)
                return helper.FormControlStatic(entityLine, entityLine.Compose(EntityBaseKeys.ToStr), null, null);

            var htmlAttr = new Dictionary<string, object>
            {
                {"class", "form-control sf-entity-autocomplete"},
                { "autocomplete", "off" }, 
            };

            if (entityLine.PlaceholderLabels)
                htmlAttr.Add("placeholder", entityLine.LabelText);

            return helper.TextBox(
                entityLine.Compose(EntityBaseKeys.ToStr),
                null,
                htmlAttr);
        }

        public static MvcHtmlString EntityLine<T,S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property) 
        {
            return helper.EntityLine<T, S>(tc, property, null);
        }

        public static MvcHtmlString EntityLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<EntityLine> settingsModifier)
        {
            TypeContext<S> context = Common.WalkExpression(tc, property);

            var vo = tc.ViewOverrides;

            if (vo != null && !vo.IsVisible(context.PropertyRoute))
                return vo.OnSurroundLine(context.PropertyRoute, helper, tc, null);
            
            EntityLine el = new EntityLine(typeof(S), context.Value, context, null, context.PropertyRoute);

            EntityBaseHelper.ConfigureEntityBase(el, el.CleanRuntimeType ?? el.Type.CleanType());

            if (el.Implementations == null || el.Implementations.Value.IsByAll)
                el.Autocomplete = false;

            Common.FireCommonTasks(el);

            if (settingsModifier != null)
                settingsModifier(el);

            var result = helper.InternalEntityLine(el); 

            if (vo == null)
                return result;

            return vo.OnSurroundLine(el.PropertyRoute, helper, tc, result);
        }
    }
}
