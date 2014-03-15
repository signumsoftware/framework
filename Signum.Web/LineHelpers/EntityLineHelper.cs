#region usings
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
#endregion

namespace Signum.Web
{
    public static class EntityLineHelper
    {
        internal static MvcHtmlString InternalEntityLine(this HtmlHelper helper, EntityLine entityLine)
        {
            if (!entityLine.Visible || (entityLine.HideIfNull && entityLine.UntypedValue == null))
                return MvcHtmlString.Empty;

            HtmlStringBuilder sb = new HtmlStringBuilder();
            using (sb.Surround(new HtmlTag("div", entityLine.Prefix).Class("SF-entity-line SF-control-container")))
            {
                 sb.AddLine(helper.HiddenRuntimeInfo(entityLine));

                 using (sb.Surround(new HtmlTag("div", entityLine.Compose("hidden")).Class("hide")))
                 {
                     if (entityLine.UntypedValue != null)
                     {
                         sb.AddLine(AutocompleteTextBox(helper, entityLine));
                         sb.AddLine(EntityBaseHelper.CreateButton(helper, entityLine));
                         sb.AddLine(EntityBaseHelper.FindButton(helper, entityLine));
                     }
                     else
                     {
                         sb.AddLine(LinkOrSpan(helper, entityLine));
                         sb.AddLine(EntityBaseHelper.ViewButton(helper, entityLine));
                         sb.AddLine(EntityBaseHelper.RemoveButton(helper, entityLine));
                     }
                 }

                 using (sb.Surround(new HtmlTag("div", entityLine.Compose("inputGroup")).Class("input-group")))
                {
                    if (entityLine.UntypedValue == null)
                        sb.AddLine(AutocompleteTextBox(helper, entityLine));
                    else
                        sb.AddLine(LinkOrSpan(helper, entityLine));

                    using (sb.Surround(new HtmlTag("span", entityLine.Compose("shownButton")).Class("input-group-btn")))
                    {
                        if (entityLine.UntypedValue == null)
                        {
                            sb.AddLine(EntityBaseHelper.CreateButton(helper, entityLine));
                            sb.AddLine(EntityBaseHelper.FindButton(helper, entityLine));
                        }
                        else
                        {
                            sb.AddLine(EntityBaseHelper.ViewButton(helper, entityLine));
                            sb.AddLine(EntityBaseHelper.RemoveButton(helper, entityLine));
                        }
                    }
                }

                if (entityLine.Type.IsEmbeddedEntity())
                {
                    TypeContext templateTC = ((TypeContext)entityLine.Parent).Clone((object)Constructor.Construct(entityLine.Type.CleanType()));
                    sb.AddLine(EntityBaseHelper.EmbeddedTemplate(entityLine, EntityBaseHelper.RenderPopup(helper, templateTC, RenderPopupMode.Popup, entityLine, isTemplate: true), null));
                }

                if (EntityBaseHelper.EmbeddedOrNew((Modifiable)entityLine.UntypedValue))
                    sb.AddLine(EntityBaseHelper.RenderPopup(helper, (TypeContext)entityLine.Parent, RenderPopupMode.PopupInDiv, entityLine));


                sb.AddLine(entityLine.ConstructorScript(JsFunction.LinesModule, "EntityLine"));
            }

            return helper.FormGroup(entityLine, entityLine.Prefix, entityLine.LabelText, sb.ToHtml());
        }

        private static MvcHtmlString LinkOrSpan(HtmlHelper helper, EntityLine entityLine)
        {
            if (entityLine.Navigate)
            {
                return helper.Href(entityLine.Compose(EntityBaseKeys.Link),
                        entityLine.UntypedValue.TryToString(),
                        entityLine.IdOrNull != null ? Navigator.NavigateRoute(entityLine.CleanRuntimeType, entityLine.IdOrNull.Value) : null,
                        JavascriptMessage.navigate.NiceToString(), "form-control  btn-default", null);
            }
            else
            {
                return helper.Span(entityLine.Compose(EntityBaseKeys.Link),
                        entityLine.UntypedValue.TryToString() ?? " ",
                        "form-control btn-default");
            }
        }

        private static MvcHtmlString AutocompleteTextBox(HtmlHelper helper, EntityLine entityLine)
        {
            if (!entityLine.Autocomplete)
                return MvcHtmlString.Empty;

            if (entityLine.Implementations.Value.IsByAll)
                throw new InvalidOperationException("Autocomplete is not possible with ImplementedByAll");

            var htmlAttr = new Dictionary<string, object>
            {
                {"class", "form-control sf-entity-autocomplete"},
                { "autocomplete", "off" }, 
            };

            return helper.TextBox(
                entityLine.Compose(EntityBaseKeys.ToStr),
                entityLine.ToStr,
                htmlAttr);
        }

        public static MvcHtmlString EntityLine<T,S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property) 
        {
            return helper.EntityLine<T, S>(tc, property, null);
        }

        public static MvcHtmlString EntityLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<EntityLine> settingsModifier)
        {
            TypeContext<S> context = Common.WalkExpression(tc, property);

            EntityLine el = new EntityLine(typeof(S), context.Value, context, null, context.PropertyRoute);

            EntityBaseHelper.ConfigureEntityBase(el, el.CleanRuntimeType ?? el.Type.CleanType());

            if (el.Implementations == null || el.Implementations.Value.IsByAll)
                el.Autocomplete = false;

            Common.FireCommonTasks(el);

            if (settingsModifier != null)
                settingsModifier(el);

            var result = helper.InternalEntityLine(el); 

            var vo = el.ViewOverrides;
            if (vo == null)
                return result;

            return vo.OnSurroundLine(el.PropertyRoute, helper, tc, result);


        }
    }
}
