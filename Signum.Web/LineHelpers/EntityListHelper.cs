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
#endregion

namespace Signum.Web
{
    public static class EntityListHelper
    {
        private static MvcHtmlString InternalEntityList<T>(this HtmlHelper helper, EntityList entityList)
        {
            if (!entityList.Visible || entityList.HideIfNull && entityList.UntypedValue == null)
                return MvcHtmlString.Empty;

            HtmlStringBuilder sb = new HtmlStringBuilder();
            using (sb.Surround(new HtmlTag("div").Id(entityList.ControlID).Class("sf-field")))
            {
                sb.AddLine(EntityBaseHelper.BaseLineLabel(helper, entityList));

                using (sb.Surround(new HtmlTag("div").Class("sf-field-list")))
                {
                    sb.AddLine(helper.HiddenStaticInfo(entityList));
                    sb.AddLine(helper.Hidden(entityList.Compose(EntityListBaseKeys.ListPresent), ""));

                    //If it's an embeddedEntity write an empty template with index 0 to be used when creating a new item
                    if (entityList.ElementType.IsEmbeddedEntity())
                    {
                        TypeElementContext<T> templateTC = new TypeElementContext<T>((T)(object)Constructor.Construct(typeof(T)), (TypeContext)entityList.Parent, 0);
                        sb.AddLine(EntityBaseHelper.EmbeddedTemplate(entityList, EntityBaseHelper.RenderPopup(helper, templateTC, RenderPopupMode.Popup, entityList, isTemplate: true)));
                    }

                    HtmlStringBuilder sbSelect = new HtmlStringBuilder();
                    
                    var sbSelectContainer = new HtmlTag("select").Attr("multiple", "multiple")
                        .IdName(entityList.Compose(EntityListBaseKeys.List))
                        .Class("sf-entity-list");

                    if (entityList.ListHtmlProps.Any())
                        sbSelectContainer.Attrs(entityList.ListHtmlProps);
                    
                    using (sbSelect.Surround(sbSelectContainer))
                    {
                        if (entityList.UntypedValue != null)
                        {
                            foreach (var itemTC in TypeContextUtilities.TypeElementContext((TypeContext<MList<T>>)entityList.Parent))
                                sb.Add(InternalListElement(helper, sbSelect, itemTC, entityList));
                        }
                    }

                    using (sb.Surround(new HtmlTag("table").Class("sf-field-list-table")))
                    using (sb.Surround(new HtmlTag("tr")))
                    {
                        using (sb.Surround(new HtmlTag("td")))
                        {
                            sb.Add(sbSelect.ToHtml());
                        }
                        
                        using (sb.Surround(new HtmlTag("td")))
                        using (sb.Surround(new HtmlTag("ul")))
                        {
                            sb.AddLine(ListBaseHelper.ViewButton(helper, entityList).Surround("li"));
                            sb.AddLine(ListBaseHelper.CreateButton(helper, entityList).Surround("li"));
                            sb.AddLine(ListBaseHelper.FindButton(helper, entityList).Surround("li"));
                            sb.AddLine(ListBaseHelper.RemoveButton(helper, entityList).Surround("li"));
                            sb.AddLine(ListBaseHelper.MoveUpButton(helper, entityList).Surround("li"));
                            sb.AddLine(ListBaseHelper.MoveDownButton(helper, entityList).Surround("li"));
                        }
                    }
                }
            }

            sb.AddLine(new HtmlTag("script").Attr("type", "text/javascript")
                .InnerHtml(new MvcHtmlString("$('#{0}').entityList({1})".Formato(entityList.ControlID, entityList.OptionsJS())))
                .ToHtml());

            return sb.ToHtml();
        }

        static MvcHtmlString InternalListElement<T>(this HtmlHelper helper, HtmlStringBuilder sbOptions, TypeElementContext<T> itemTC, EntityList entityList)
        {
            HtmlStringBuilder sb = new HtmlStringBuilder();

            sb.AddLine(ListBaseHelper.WriteIndex(helper, entityList, itemTC, itemTC.Index));
            sb.AddLine(helper.HiddenRuntimeInfo(itemTC));

            if (EntityBaseHelper.EmbeddedOrNew((Modifiable)(object)itemTC.Value))
                sb.AddLine(EntityBaseHelper.RenderPopup(helper, itemTC, RenderPopupMode.PopupInDiv, entityList));
            else if (itemTC.Value != null)
                sb.Add(helper.Div(itemTC.Compose(EntityBaseKeys.Entity), null, "", new Dictionary<string, object> { { "style", "display:none" }, { "class", "sf-entity-list" } }));
            
            //Note this is added to the sbOptions, not to the result sb

            sbOptions.Add(new HtmlTag("option", itemTC.Compose(EntityBaseKeys.ToStr))
                                .Attrs(new
                                {
                                    name = itemTC.Compose(EntityBaseKeys.ToStr),
                                    value = ""
                                })
                                .Class("sf-value-line")
                                .Class("sf-entity-list-option")
                                .SetInnerText(
                                    (itemTC.Value as IIdentifiable).TryCC(i => i.ToString()) ??
                                    (itemTC.Value as Lite<IIdentifiable>).TryCC(i => i.ToString()) ??
                                    (itemTC.Value as EmbeddedEntity).TryCC(i => i.ToString()) ?? "")
                                .ToHtml(TagRenderMode.Normal));
            
            return sb.ToHtml();
        }

        public static MvcHtmlString EntityList<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property)
        {
            return helper.EntityList<T, S>(tc, property, null);
        }

        public static MvcHtmlString EntityList<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property, Action<EntityList> settingsModifier)
        {
            TypeContext<MList<S>> context = Common.WalkExpression(tc, property);

            EntityList el = new EntityList(context.Type, context.UntypedValue, context, null, context.PropertyRoute);

            EntityBaseHelper.ConfigureEntityBase(el, typeof(S).CleanType());

            Common.FireCommonTasks(el);

            if (settingsModifier != null)
                settingsModifier(el);

            var result = helper.InternalEntityList<S>(el);

            var vo = el.ViewOverrides;
            if (vo == null)
                return result;

            return vo.OnSurroundLine(el.PropertyRoute, helper, tc, result);
        }
    }
}
