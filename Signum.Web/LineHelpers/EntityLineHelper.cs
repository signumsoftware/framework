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
        internal static string InternalEntityLine(this HtmlHelper helper, EntityLine entityLine)
        {
            if (!entityLine.Visible || (entityLine.HideIfNull && entityLine.UntypedValue == null))
                return null;

            StringBuilder sb = new StringBuilder();
            if (entityLine.ShowFieldDiv)
                sb.AppendLine("<div class='field'>");

            sb.AppendLine(EntityBaseHelper.BaseLineLabel(helper, entityLine));

            sb.AppendLine(helper.HiddenEntityInfo(entityLine));

            if (entityLine.Type.IsIIdentifiable() || entityLine.Type.IsLite())
            {
                sb.AppendLine(EntityBaseHelper.WriteImplementations(helper, entityLine));

                if (EntityBaseHelper.RequiresLoadAll(helper, entityLine))
                    sb.AppendLine(EntityBaseHelper.RenderTypeContext(helper, (TypeContext)entityLine.Parent, RenderMode.PopupInDiv, entityLine.PartialViewName, entityLine.ReloadOnChange));
                else if (entityLine.UntypedValue != null)
                    sb.AppendLine(helper.Div(entityLine.Compose(EntityBaseKeys.Entity), "", "", new Dictionary<string, object> { { "style", "display:none" } }));
                
                sb.AppendLine(helper.TextBox(
                    entityLine.Compose(EntityBaseKeys.ToStr),
                    entityLine.ToStr, 
                    new Dictionary<string, object>() 
                    { 
                        { "class", "valueLine" }, 
                        { "autocomplete", "off" }, 
                        { "style", "display:" + ((entityLine.UntypedValue==null && !entityLine.ReadOnly) ? "block" : "none")}
                    }).ToHtmlString());

                if (entityLine.Autocomplete)
                {
                    if (entityLine.Implementations != null && entityLine.Implementations.IsByAll)
                        throw new InvalidOperationException("Autocomplete is not possible with ImplementedByAll");

                    sb.AppendLine(helper.AutoCompleteExtender(entityLine.Compose(EntityBaseKeys.ToStr),
                                     Navigator.GetName(entityLine.Type.CleanType()),
                                     ImplementationsModelBinder.Render(entityLine.Implementations),
                                     entityLine.Compose("sfId"),
                                     "Signum/Autocomplete", entityLine.OnChangedTotal.HasText() ? entityLine.OnChangedTotal : "''"));

                }
            }
            else
            {
                if (entityLine.UntypedValue == null)
                {
                    TypeContext templateTC = ((TypeContext)entityLine.Parent).Clone((object)Constructor.Construct(entityLine.Type.CleanType()));
                    sb.AppendLine(EntityBaseHelper.EmbeddedTemplate(entityLine, EntityBaseHelper.RenderTypeContext(helper, templateTC, RenderMode.Popup, entityLine.PartialViewName, entityLine.ReloadOnChange)));
                }

                if (entityLine.UntypedValue != null)
                    sb.AppendLine(EntityBaseHelper.RenderTypeContext(helper, (TypeContext)entityLine.Parent, RenderMode.PopupInDiv, entityLine.PartialViewName, entityLine.ReloadOnChange));

                sb.AppendLine(helper.Span(entityLine.Compose(EntityBaseKeys.ToStrLink), entityLine.UntypedValue.TryToString(), "valueLine"));
            }

            int? id = entityLine.IdOrNull;
            if (entityLine.Navigate && id != null)
            {
                sb.AppendLine(
                    helper.Href(entityLine.Compose(EntityBaseKeys.ToStrLink),
                        entityLine.UntypedValue.ToString(), Navigator.ViewRoute(entityLine.CleanRuntimeType, id), Resources.View, "valueLine",
                        new Dictionary<string, object> { { "style", "display:" + ((entityLine.UntypedValue == null) ? "none" : "block") } }));
            }
            else if (entityLine.Type.IsIIdentifiable() || entityLine.Type.IsLite())
            {
                sb.AppendLine(
                    helper.Span(entityLine.Compose(EntityBaseKeys.ToStrLink),
                        entityLine.UntypedValue.TryToString() ?? "&nbsp;",
                        "valueLine",
                        new Dictionary<string, object> { { "style", "display:" + ((entityLine.UntypedValue == null) ? "none" : "block") } }));
            }

            sb.AppendLine(EntityBaseHelper.WriteViewButton(helper, entityLine));
            sb.AppendLine(EntityBaseHelper.WriteCreateButton(helper, entityLine));
            sb.AppendLine(EntityBaseHelper.WriteFindButton(helper, entityLine));
            sb.AppendLine(EntityBaseHelper.WriteRemoveButton(helper, entityLine));

            if (entityLine.ShowFieldDiv)
                sb.AppendLine("</div>");

            sb.AppendLine(EntityBaseHelper.WriteBreakLine(helper, entityLine));

            return sb.ToString();
        }

        public static void EntityLine<T,S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property) 
        {
            helper.EntityLine<T, S>(tc, property, null);
        }

        public static void EntityLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<EntityLine> settingsModifier)
        {
            TypeContext<S> context = Common.WalkExpression(tc, property);

            EntityLine el = new EntityLine(typeof(S), context.Value, context, null, context.PropertyRoute);

            EntityBaseHelper.ConfigureEntityBase(el, el.CleanRuntimeType ?? el.Type.CleanType());

            Common.FireCommonTasks(el);

            if (settingsModifier != null)
                settingsModifier(el);

            helper.Write(helper.InternalEntityLine(el));
        }
    }
}
