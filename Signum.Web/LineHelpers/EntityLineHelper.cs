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
using Signum.Web.Properties;
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
            using (entityLine.ShowFieldDiv ? sb.Surround(new HtmlTag("div").Class("sf-field")) : null)
            using (entityLine.ValueFirst ? sb.Surround(new HtmlTag("div").Class("sf-value-first")) : null)
            {
                if (!entityLine.ValueFirst)
                    sb.AddLine(EntityBaseHelper.BaseLineLabel(helper, entityLine));

                using (sb.Surround(new HtmlTag("div").Class("sf-value-container")))
                {
                    sb.AddLine(helper.HiddenEntityInfo(entityLine));

                    if (entityLine.Type.IsIIdentifiable() || entityLine.Type.IsLite())
                    {
                        if (EntityBaseHelper.RequiresLoadAll(helper, entityLine))
                            sb.AddLine(EntityBaseHelper.RenderTypeContext(helper, (TypeContext)entityLine.Parent, RenderMode.PopupInDiv, entityLine));
                        else if (entityLine.UntypedValue != null)
                            sb.AddLine(helper.Div(entityLine.Compose(EntityBaseKeys.Entity), null, "", new Dictionary<string, object> { { "style", "display:none" } }));

                        if (entityLine.Autocomplete && entityLine.Implementations != null && entityLine.Implementations.IsByAll)
                            throw new InvalidOperationException("Autocomplete is not possible with ImplementedByAll");

                        var htmlAttr = new Dictionary<string, object>
                        {
                            {"class", "sf-value-line" + (entityLine.Autocomplete ? " sf-entity-autocomplete" : "")},
                            { "autocomplete", "off" }, 
                            { "style", "display:" + ((entityLine.UntypedValue==null && !entityLine.ReadOnly) ? "block" : "none")}
                        };

                        if (entityLine.Autocomplete)
                        {
                            htmlAttr.AddRange(new Dictionary<string, object>
                            {
                                { "data-url", helper.UrlHelper().Action("Autocomplete", "Signum") },
                                { "data-types", new StaticInfo(entityLine.Type, entityLine.Implementations).Types.ToString(t => Navigator.ResolveWebTypeName(t), ",") }
                            });
                        }

                        sb.AddLine(helper.TextBox(
                            entityLine.Compose(EntityBaseKeys.ToStr),
                            entityLine.ToStr,
                            htmlAttr));
                    }
                    else
                    {
                        if (entityLine.UntypedValue == null && entityLine.Parent is TypeContext) /*Second condition filters embedded entities in filters to be rendered */
                        {
                            TypeContext templateTC = ((TypeContext)entityLine.Parent).Clone((object)Constructor.Construct(entityLine.Type.CleanType()));
                            sb.AddLine(EntityBaseHelper.EmbeddedTemplate(entityLine, EntityBaseHelper.RenderTypeContext(helper, templateTC, RenderMode.Popup, entityLine)));
                        }

                        if (entityLine.UntypedValue != null)
                            sb.AddLine(EntityBaseHelper.RenderTypeContext(helper, (TypeContext)entityLine.Parent, RenderMode.PopupInDiv, entityLine));

                        sb.AddLine(helper.Span(entityLine.Compose(EntityBaseKeys.ToStrLink), entityLine.UntypedValue.TryToString(), "sf-value-line"));
                    }

                    int? id = entityLine.IdOrNull;
                    if (entityLine.Navigate && id != null)
                    {
                        sb.AddLine(
                            helper.Href(entityLine.Compose(EntityBaseKeys.ToStrLink),
                                entityLine.UntypedValue.ToString(), Navigator.ViewRoute(entityLine.CleanRuntimeType, id), Resources.View, "sf-value-line",
                                new Dictionary<string, object> { { "style", "display:" + ((entityLine.UntypedValue == null) ? "none" : "block") } }));
                    }
                    else if (entityLine.Type.IsIIdentifiable() || entityLine.Type.IsLite())
                    {
                        sb.AddLine(
                            helper.Span(entityLine.Compose(EntityBaseKeys.ToStrLink),
                                entityLine.UntypedValue.TryToString() ?? " ",
                                "sf-value-line",
                                new Dictionary<string, object> { { "style", "display:" + ((entityLine.UntypedValue == null) ? "none" : "block") } }));
                    }

                    sb.AddLine(EntityBaseHelper.ViewButton(helper, entityLine));
                    sb.AddLine(EntityBaseHelper.CreateButton(helper, entityLine));
                    sb.AddLine(EntityBaseHelper.FindButton(helper, entityLine));
                    sb.AddLine(EntityBaseHelper.RemoveButton(helper, entityLine));

                    if (entityLine.ShowValidationMessage)
                    {
                        sb.AddLine(helper.ValidationMessage(entityLine.ControlID));
                    }

                }

                if (entityLine.ValueFirst)
                    sb.AddLine(EntityBaseHelper.BaseLineLabel(helper, entityLine));
            }

            return sb.ToHtml();
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

            if (el.Implementations.TryCS(i => i.IsByAll) == true)
                el.Autocomplete = false;

            Common.FireCommonTasks(el);

            if (settingsModifier != null)
                settingsModifier(el);

            return helper.InternalEntityLine(el);
        }
    }
}
