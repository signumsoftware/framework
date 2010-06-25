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
using Signum.Web.Properties;
#endregion

namespace Signum.Web
{
    public static class EntityListHelper
    {
        private static string InternalEntityList<T>(this HtmlHelper helper, EntityList entityList)
        {
            if (!entityList.Visible || entityList.HideIfNull && entityList.UntypedValue == null)
                return "";
            
            StringBuilder sb = new StringBuilder();
            if (entityList.ShowFieldDiv)
                sb.AppendLine("<div class='field'>");

            sb.AppendLine(EntityBaseHelper.BaseLineLabel(helper, entityList));

            sb.AppendLine(helper.Hidden(entityList.Compose(EntityBaseKeys.StaticInfo), new StaticInfo(entityList.ElementType.CleanType()) { IsReadOnly = entityList.ReadOnly }.ToString(), new { disabled = "disabled" }).ToHtmlString());
            sb.AppendLine(helper.Hidden(entityList.Compose(TypeContext.Ticks), EntityInfoHelper.GetTicks(helper, entityList).TryToString() ?? "").ToHtmlString());
            
            sb.AppendLine(EntityBaseHelper.WriteImplementations(helper, entityList));

            //If it's an embeddedEntity write an empty template with index 0 to be used when creating a new item
            if (entityList.ElementType.IsEmbeddedEntity())
            {
                TypeElementContext<T> templateTC = new TypeElementContext<T>((T)(object)Constructor.Construct(typeof(T)), (TypeContext)entityList.Parent, 0);
                sb.AppendLine(EntityBaseHelper.EmbeddedTemplate(entityList, EntityBaseHelper.RenderTypeContext(helper, templateTC, RenderMode.Popup, entityList.PartialViewName, entityList.ReloadOnChange)));
            }

            if (entityList.ShowFieldDiv)
                sb.AppendLine("<div class='fieldList'>");

            StringBuilder sbSelect = new StringBuilder();
            sbSelect.AppendLine("<select id='{0}' name='{0}' multiple='multiple' ondblclick=\"{1}\" class='entityList'>".Formato(entityList.ControlID, entityList.GetViewing()));
            if (entityList.UntypedValue != null)
            {
                foreach (var itemTC in TypeContextUtilities.TypeElementContext((TypeContext<MList<T>>)entityList.Parent))
                    sb.Append(InternalListElement(helper, sbSelect, itemTC, entityList));
            }
            sbSelect.AppendLine("</select>");

            sb.Append(sbSelect);

            StringBuilder sbBtns = new StringBuilder();
            string buttonContent = ListBaseHelper.WriteCreateButton(helper, entityList, null);
            if (buttonContent.HasText())
                sbBtns.AppendLine("<tr><td>" + buttonContent + "</td></tr>");

            buttonContent = ListBaseHelper.WriteFindButton(helper, entityList);
            if (buttonContent.HasText())
                sbBtns.AppendLine("<tr><td>" + buttonContent + "</td></tr>");

            buttonContent = ListBaseHelper.WriteRemoveButton(helper, entityList);
            if (buttonContent.HasText())
                sbBtns.AppendLine("<tr><td>" + buttonContent + "</td></tr>");
            
            string sBtns = sbBtns.ToString();
            if (sBtns.HasText())
                sb.AppendLine("<table>\n" + sBtns + "</table>");

            if (entityList.ShowFieldDiv)
                sb.Append("</div>");

            sb.AppendLine(EntityBaseHelper.WriteBreakLine(helper, entityList));

            if (entityList.ShowFieldDiv)
                sb.AppendLine("</div>");

            return sb.ToString();
        }

        private static string InternalListElement<T>(this HtmlHelper helper, StringBuilder sbOptions, TypeElementContext<T> itemTC, EntityList entityList)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(helper.Hidden(itemTC.Compose(EntityListBaseKeys.Index), itemTC.Index.ToString()).ToHtmlString());

            sb.AppendLine(helper.HiddenRuntimeInfo(itemTC));

            if (typeof(T).IsEmbeddedEntity() || EntityBaseHelper.RequiresLoadAll(helper, entityList))
                sb.AppendLine(EntityBaseHelper.RenderTypeContext(helper, itemTC, RenderMode.PopupInDiv, entityList.PartialViewName, entityList.ReloadOnChange));
            else if (itemTC.Value != null)
                sb.Append(helper.Div(itemTC.Compose(EntityBaseKeys.Entity), "", "", new Dictionary<string, object> { { "style", "display:none" }, {"class", "entityList"}}));
            
            //Note this is added to the sbOptions, not to the result sb

            sbOptions.AppendLine(new FluentTagBuilder("option", itemTC.Compose(EntityBaseKeys.ToStr))
                                .MergeAttributes(new {
                                    name    = itemTC.Compose(EntityBaseKeys.ToStr),
                                    value   = ""                                    
                                })
                                .AddCssClass("valueLine")
                                .AddCssClass("entityListOption")
                                .SetInnerText(
                                    (itemTC.Value as IIdentifiable).TryCC(i => i.ToString()) ??
                                    (itemTC.Value as Lite).TryCC(i => i.ToStr) ?? 
                                    (itemTC.Value as EmbeddedEntity).TryCC(i => i.ToString()) ?? "")
                                .ToString(TagRenderMode.Normal));
            
            return sb.ToString();
        }

        public static void EntityList<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property)
        {
            helper.EntityList<T, S>(tc, property, null);
        }

        public static void EntityList<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property, Action<EntityList> settingsModifier)
        {
            TypeContext<MList<S>> context = Common.WalkExpression(tc, property);

            EntityList el = new EntityList(context.Type, context.UntypedValue, context, null, context.PropertyRoute);

            Common.FireCommonTasks(el);

            EntityBaseHelper.ConfigureEntityBase(el, Reflector.ExtractLite(typeof(S)) ?? typeof(S));

            if (settingsModifier != null)
                settingsModifier(el);

            helper.Write(helper.InternalEntityList<S>(el));
        }
    }
}
