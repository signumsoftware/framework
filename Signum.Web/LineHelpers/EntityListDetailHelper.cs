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
        private static MvcHtmlString InternalEntityListDetail<T>(this HtmlHelper helper, EntityListDetail listDetail)
        {
            if (!listDetail.Visible || listDetail.HideIfNull && listDetail.UntypedValue == null)
                return MvcHtmlString.Empty;

            string defaultDetailDiv = listDetail.Compose(EntityBaseKeys.Detail);
            if (!listDetail.DetailDiv.HasText())
                listDetail.DetailDiv = defaultDetailDiv;

            HtmlStringBuilder sb = new HtmlStringBuilder();

            sb.AddLine(EntityBaseHelper.BaseLineLabel(helper, listDetail));

            sb.AddLine(helper.HiddenStaticInfo(listDetail));

            //If it's an embeddedEntity write an empty template with index 0 to be used when creating a new item
            if (listDetail.ElementType.IsEmbeddedEntity())
            {
                TypeElementContext<T> templateTC = new TypeElementContext<T>((T)(object)Constructor.Construct(typeof(T)), (TypeContext)listDetail.Parent, 0);
                sb.AddLine(EntityBaseHelper.EmbeddedTemplate(listDetail, EntityBaseHelper.RenderTypeContext(helper, templateTC, RenderMode.Content, listDetail)));
            }

            using (listDetail.ShowFieldDiv ? sb.Surround(new HtmlTag("div").Class("sf-field-list")) : null)
            {
                HtmlStringBuilder sbSelect = new HtmlStringBuilder();
                using (sbSelect.Surround(new HtmlTag("select").IdName(listDetail.ControlID).Attr("multiple", "multiple").Attr("ondblclick", listDetail.GetViewing()).Class("sf-entity-list")))
                {
                    if (listDetail.UntypedValue != null)
                    {
                        foreach (var itemTC in TypeContextUtilities.TypeElementContext((TypeContext<MList<T>>)listDetail.Parent))
                            sb.Add(InternalListDetailElement(helper, sbSelect, itemTC, listDetail));
                    }
                }

                sb.Add(sbSelect.ToHtml());

                using (sb.Surround(new HtmlTag("ul")))
                {
                    sb.AddLine(ListBaseHelper.CreateButton(helper, listDetail, null).Surround("li"));
                    sb.AddLine(ListBaseHelper.FindButton(helper, listDetail).Surround("li"));
                    sb.AddLine(ListBaseHelper.RemoveButton(helper, listDetail).Surround("li"));
                }
            }

            if (listDetail.DetailDiv == defaultDetailDiv)
                sb.AddLine(helper.Div(listDetail.DetailDiv, null, "sf-entity-list-detail"));

            if (listDetail.UntypedValue != null && ((IList)listDetail.UntypedValue).Count > 0)
                sb.AddLine(MvcHtmlString.Create("<script type=\"text/javascript\">\n" +
                        "$(document).ready(function() {" +
                        "$('#" + listDetail.ControlID + "').dblclick();\n" +
                        "});" +
                        "</script>"));

            return sb.ToHtml();
        }

        private static MvcHtmlString InternalListDetailElement<T>(this HtmlHelper helper, HtmlStringBuilder sbOptions, TypeElementContext<T> itemTC, EntityListDetail listDetail)
        {
            HtmlStringBuilder sb = new HtmlStringBuilder();

            if (listDetail.ShouldWriteOldIndex(itemTC))
                sb.AddLine(helper.Hidden(itemTC.Compose(EntityListBaseKeys.Index), itemTC.Index.ToString()));

            sb.AddLine(helper.HiddenRuntimeInfo(itemTC));

            //TODO: Anto - RequestLoadAll con ItemTC
            if (typeof(T).IsEmbeddedEntity() || 
                EntityBaseHelper.RequiresLoadAll(helper, listDetail) ||
                (itemTC.Value.GetType().IsIIdentifiable() && (itemTC.Value as IIdentifiable).IsNew))
                sb.AddLine(EntityBaseHelper.RenderTypeContext(helper, itemTC, RenderMode.ContentInInvisibleDiv, listDetail));
            else if (itemTC.Value != null)
                sb.Add(helper.Div(itemTC.Compose(EntityBaseKeys.Entity), null, "", new Dictionary<string, object> { { "style", "display:none" } }));

            //Note this is added to the sbOptions, not to the result sb
            HtmlTag tbOption = new HtmlTag("option", itemTC.Compose(EntityBaseKeys.ToStr))
                    .Attrs(new
                    {
                        name = itemTC.Compose(EntityBaseKeys.ToStr),
                        value = ""
                    })
                    .Class("sf-value-line")
                    .Class("sf-entity-list-option")
                    .SetInnerText(
                        (itemTC.Value as IIdentifiable).TryCC(i => i.ToString()) ??
                        (itemTC.Value as Lite).TryCC(i => i.ToStr) ??
                        (itemTC.Value as EmbeddedEntity).TryCC(i => i.ToString()) ?? "");

            if (itemTC.Index == 0)
                tbOption.Attr("selected", "selected");


            sbOptions.Add(tbOption.ToHtml());

            return sb.ToHtml();
        }

        public static MvcHtmlString EntityListDetail<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property)
        {
            return helper.EntityListDetail<T, S>(tc, property, null);
        }

        public static MvcHtmlString EntityListDetail<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property, Action<EntityListDetail> settingsModifier)
        {
            TypeContext<MList<S>> context = Common.WalkExpression(tc, property);

            EntityListDetail el = new EntityListDetail(context.Type, context.UntypedValue, context, null, context.PropertyRoute);

            EntityBaseHelper.ConfigureEntityBase(el, Reflector.ExtractLite(typeof(S)) ?? typeof(S));

            Common.FireCommonTasks(el);

            if (settingsModifier != null)
                settingsModifier(el);

            return helper.InternalEntityListDetail<S>(el);
        }
    }
}
