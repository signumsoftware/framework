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
    public static class EntityListDetailHelper
    {
        private static string InternalEntityListDetail<T>(this HtmlHelper helper, EntityListDetail listDetail)
        {
            if (!listDetail.Visible || listDetail.HideIfNull && listDetail.UntypedValue == null)
                return "";

            string defaultDetailDiv = listDetail.Compose(EntityBaseKeys.Detail);
            if (!listDetail.DetailDiv.HasText())
                listDetail.DetailDiv = defaultDetailDiv;

            StringBuilder sb = new StringBuilder();

            sb.AppendLine(EntityBaseHelper.BaseLineLabel(helper, listDetail));

            sb.AppendLine(helper.Hidden(listDetail.Compose(EntityBaseKeys.StaticInfo), new StaticInfo(listDetail.ElementType.CleanType()) { IsReadOnly = listDetail.ReadOnly }.ToString(), new { disabled = "disabled" }).ToHtmlString());
            sb.AppendLine(helper.Hidden(listDetail.Compose(TypeContext.Ticks), EntityInfoHelper.GetTicks(helper, listDetail).TryToString() ?? "").ToHtmlString());
            
            sb.AppendLine(EntityBaseHelper.WriteImplementations(helper, listDetail));

            //If it's an embeddedEntity write an empty template with index 0 to be used when creating a new item
            if (listDetail.ElementType.IsEmbeddedEntity())
            {
                TypeElementContext<T> templateTC = new TypeElementContext<T>((T)(object)Constructor.Construct(typeof(T)), (TypeContext)listDetail.Parent, 0);
                sb.AppendLine(EntityBaseHelper.EmbeddedTemplate(listDetail, EntityBaseHelper.RenderTypeContext(helper, templateTC, RenderMode.Content, listDetail.PartialViewName, listDetail.ReloadOnChange)));
            }

            if (listDetail.ShowFieldDiv)
                sb.AppendLine("<div class='fieldList'>");

            StringBuilder sbSelect = new StringBuilder();
            sbSelect.AppendLine("<select id='{0}' name='{0}' multiple='multiple' ondblclick=\"{1}\" class='entityList'>".Formato(listDetail.ControlID, listDetail.GetViewing()));
            if (listDetail.UntypedValue != null)
            {
                foreach (var itemTC in TypeContextUtilities.TypeElementContext((TypeContext<MList<T>>)listDetail.Parent))
                    sb.Append(InternalListDetailElement(helper, sbSelect, itemTC, listDetail));
            }
            sbSelect.AppendLine("</select>");

            sb.Append(sbSelect);

            StringBuilder sbBtns = new StringBuilder();
            sbBtns.AppendLine("<tr><td>" + ListBaseHelper.WriteCreateButton(helper, listDetail, null) + "</td></tr>");
            sbBtns.AppendLine("<tr><td>" + ListBaseHelper.WriteFindButton(helper, listDetail) + "</td></tr>");
            sbBtns.AppendLine("<tr><td>" + ListBaseHelper.WriteRemoveButton(helper, listDetail) + "</td></tr>");

            string sBtns = sbBtns.ToString();
            if (sBtns.HasText())
                sb.AppendLine("<table>\n" + sBtns + "</table>");

            if (listDetail.ShowFieldDiv)
                sb.Append("</div>");

            sb.AppendLine(EntityBaseHelper.WriteBreakLine(helper, listDetail));

            if (listDetail.DetailDiv == defaultDetailDiv)
                sb.AppendLine(helper.Div(listDetail.DetailDiv, "", ""));

            if (listDetail.UntypedValue != null && ((IList)listDetail.UntypedValue).Count > 0)
                sb.AppendLine("<script type=\"text/javascript\">\n" +
                        "$(document).ready(function() {" +
                        "$('#" + listDetail.ControlID + "').dblclick();\n" +
                        "});" +
                        "</script>");

            sb.AppendLine(EntityBaseHelper.WriteBreakLine(helper, listDetail));

            return sb.ToString();
        }

        private static string InternalListDetailElement<T>(this HtmlHelper helper, StringBuilder sbOptions, TypeElementContext<T> itemTC, EntityListDetail listDetail)
        {
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine(helper.Hidden(itemTC.Compose(EntityListBaseKeys.Index), itemTC.Index.ToString()).ToHtmlString());

            sb.AppendLine(helper.HiddenRuntimeInfo(itemTC));

            if (typeof(T).IsEmbeddedEntity() || EntityBaseHelper.RequiresLoadAll(helper, listDetail))
                sb.AppendLine(EntityBaseHelper.RenderTypeContext(helper, itemTC, RenderMode.ContentInInvisibleDiv, listDetail.PartialViewName, listDetail.ReloadOnChange));
            else if (itemTC.Value != null)
                sb.Append(helper.Div(itemTC.Compose(EntityBaseKeys.Entity), "", "", new Dictionary<string, object> { { "style", "display:none" } }));

            //Note this is added to the sbOptions, not to the result sb
            FluentTagBuilder tbOption = new FluentTagBuilder("option", itemTC.Compose(EntityBaseKeys.ToStr))
                    .MergeAttributes(new
                    {
                        name = itemTC.Compose(EntityBaseKeys.ToStr),
                        value = ""
                    })
                    .AddCssClass("valueLine")
                    .AddCssClass("entityListOption")
                    .SetInnerText(
                        (itemTC.Value as IIdentifiable).TryCC(i => i.ToString()) ??
                        (itemTC.Value as Lite).TryCC(i => i.ToStr) ??
                        (itemTC.Value as EmbeddedEntity).TryCC(i => i.ToString()) ?? "");

            if (itemTC.Index == 0)
                tbOption.MergeAttribute("selected", "selected");

            
            sbOptions.AppendLine(tbOption.ToString(TagRenderMode.Normal));

            return sb.ToString();
        }

        public static void EntityListDetail<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property)
        {
            helper.EntityListDetail<T, S>(tc, property, null);
        }

        public static void EntityListDetail<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property, Action<EntityListDetail> settingsModifier)
        {
            TypeContext<MList<S>> context = Common.WalkExpression(tc, property);

            EntityListDetail el = new EntityListDetail(context.Type, context.UntypedValue, context, null, context.PropertyRoute);

            Common.FireCommonTasks(el);

            EntityBaseHelper.ConfigureEntityBase(el, Reflector.ExtractLite(typeof(S)) ?? typeof(S));

            if (settingsModifier != null)
                settingsModifier(el);

            helper.Write(helper.InternalEntityListDetail<S>(el));
        }
    }
}
