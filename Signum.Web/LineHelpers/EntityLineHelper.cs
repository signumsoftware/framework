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

            using (sb.Surround(new HtmlTag("div").Class("input-group")))
            {
                sb.AddLine(helper.HiddenRuntimeInfo(entityLine));
                if (entityLine.Type.IsIIdentifiable() || entityLine.Type.IsLite())
                {
                    if (entityLine.Autocomplete && entityLine.Implementations.Value.IsByAll)
                        throw new InvalidOperationException("Autocomplete is not possible with ImplementedByAll");

                    if (entityLine.Autocomplete)
                    {
                        var htmlAttr = new Dictionary<string, object>
                        {
                            {"class", "form-control" + (entityLine.Autocomplete ? " sf-entity-autocomplete" : "")},
                            { "autocomplete", "off" }, 
                            { "style", "display:" + ((entityLine.UntypedValue==null && !entityLine.ReadOnly) ? "block" : "none")}
                        };

                        sb.AddLine(helper.TextBox(
                            entityLine.Compose(EntityBaseKeys.ToStr),
                            entityLine.ToStr,
                            htmlAttr));
                    }

                    if (entityLine.Navigate)
                    {
                        sb.AddLine(
                            helper.Href(entityLine.Compose(EntityBaseKeys.Link),
                                entityLine.UntypedValue.TryToString(),
                                entityLine.IdOrNull != null ? Navigator.NavigateRoute(entityLine.CleanRuntimeType, entityLine.IdOrNull.Value) : null,
                                JavascriptMessage.navigate.NiceToString(), "form-control  btn-default",
                                new Dictionary<string, object> { { "style", "display:" + ((entityLine.UntypedValue == null) ? "none" : "block") } }));
                    }
                    else
                    {
                        sb.AddLine(
                            helper.Span(entityLine.Compose(EntityBaseKeys.Link),
                                entityLine.UntypedValue.TryToString() ?? " ",
                                "form-control btn-default",
                                new Dictionary<string, object> { { "style", "display:" + ((entityLine.UntypedValue == null) ? "none" : "block") } }));
                    }
                }
                else
                {
                    sb.AddLine(helper.Span(entityLine.Compose(EntityBaseKeys.Link), entityLine.UntypedValue.TryToString(), "form-control  btn-default"));
                }

                using (sb.Surround(new HtmlTag("span").Class("input-group-btn")))
                {
                    sb.AddLine(EntityBaseHelper.ViewButton(helper, entityLine, hidden: entityLine.UntypedValue == null));
                    sb.AddLine(EntityBaseHelper.CreateButton(helper, entityLine, hidden: entityLine.UntypedValue != null));
                    sb.AddLine(EntityBaseHelper.FindButton(helper, entityLine, hidden: entityLine.UntypedValue != null));
                    sb.AddLine(EntityBaseHelper.RemoveButton(helper, entityLine, hidden: entityLine.UntypedValue == null));
                }
            } 
            
            if (entityLine.ShowValidationMessage)
            {
                sb.AddLine(helper.ValidationMessage(entityLine.Prefix));
            }

            if (entityLine.Type.IsEmbeddedEntity())
            {
                TypeContext templateTC = ((TypeContext)entityLine.Parent).Clone((object)Constructor.Construct(entityLine.Type.CleanType()));
                sb.AddLine(EntityBaseHelper.EmbeddedTemplate(entityLine, EntityBaseHelper.RenderPopup(helper, templateTC, RenderPopupMode.Popup, entityLine, isTemplate: true), null));
            }

            if (EntityBaseHelper.EmbeddedOrNew((Modifiable)entityLine.UntypedValue))
                sb.AddLine(EntityBaseHelper.RenderPopup(helper, (TypeContext)entityLine.Parent, RenderPopupMode.PopupInDiv, entityLine));


            sb.AddLine(entityLine.ConstructorScript(JsFunction.LinesModule, "EntityLine"));

            return helper.FormGroup(entityLine, entityLine.Prefix, entityLine.LabelText, sb.ToHtml());
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
