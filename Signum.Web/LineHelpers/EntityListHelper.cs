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
        private static MvcHtmlString InternalEntityList<T>(this HtmlHelper helper, EntityList entityList)
        {
            if (!entityList.Visible || entityList.HideIfNull && entityList.UntypedValue == null)
                return MvcHtmlString.Empty;

            HtmlStringBuilder sb = new HtmlStringBuilder();
            using (entityList.ShowFieldDiv ? sb.Surround(new HtmlTag("div").Class("sf-field")) : null)
            {
                sb.AddLine(EntityBaseHelper.BaseLineLabel(helper, entityList));

                sb.AddLine(helper.HiddenStaticInfo(entityList));

                //If it's an embeddedEntity write an empty template with index 0 to be used when creating a new item
                if (entityList.ElementType.IsEmbeddedEntity())
                {
                    TypeElementContext<T> templateTC = new TypeElementContext<T>((T)(object)Constructor.Construct(typeof(T)), (TypeContext)entityList.Parent, 0);
                    sb.AddLine(EntityBaseHelper.EmbeddedTemplate(entityList, EntityBaseHelper.RenderTypeContext(helper, templateTC, RenderMode.Popup, entityList)));
                }

                using (entityList.ShowFieldDiv ? sb.Surround(new HtmlTag("div").Class("sf-field-list")) : null)
                {
                    HtmlStringBuilder sbSelect = new HtmlStringBuilder();
                    
                    var sbSelectContainer = new HtmlTag("select").IdName(entityList.ControlID).Class("sf-entity-list").Attr("multiple", "multiple");
                    
                    using (sbSelect.Surround(sbSelectContainer))
                    {
                        if (entityList.UntypedValue != null)
                        {
                            foreach (var itemTC in TypeContextUtilities.TypeElementContext((TypeContext<MList<T>>)entityList.Parent))
                                sb.Add(InternalListElement(helper, sbSelect, itemTC, entityList));
                        }
                    }

                    sb.Add(sbSelect.ToHtml());

                    using (sb.Surround(new HtmlTag("ul")))
                    {
                        sb.AddLine(ListBaseHelper.ViewButton(helper, entityList).Surround("li"));
                        sb.AddLine(ListBaseHelper.CreateButton(helper, entityList, null).Surround("li"));
                        sb.AddLine(ListBaseHelper.FindButton(helper, entityList).Surround("li"));
                        sb.AddLine(ListBaseHelper.RemoveButton(helper, entityList).Surround("li"));
                    }
                }
            }

            return sb.ToHtml();
        }

        static MvcHtmlString InternalListElement<T>(this HtmlHelper helper, HtmlStringBuilder sbOptions, TypeElementContext<T> itemTC, EntityList entityList)
        {
            HtmlStringBuilder sb = new HtmlStringBuilder();

            if (entityList.ShouldWriteOldIndex(itemTC))
                sb.AddLine(helper.Hidden(itemTC.Compose(EntityListBaseKeys.Index), itemTC.Index.ToString()));

            sb.AddLine(helper.HiddenRuntimeInfo(itemTC));

            if (typeof(T).IsEmbeddedEntity() || 
                EntityBaseHelper.RequiresLoadAll(helper, entityList) || 
                (itemTC.Value.GetType().IsIIdentifiable() && (itemTC.Value as IIdentifiable).IsNew))
                sb.AddLine(EntityBaseHelper.RenderTypeContext(helper, itemTC, RenderMode.PopupInDiv, entityList));
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
                                    (itemTC.Value as Lite).TryCC(i => i.ToStr) ??
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

            EntityBaseHelper.ConfigureEntityBase(el, Reflector.ExtractLite(typeof(S)) ?? typeof(S));

            Common.FireCommonTasks(el);

            if (settingsModifier != null)
                settingsModifier(el);

            return helper.InternalEntityList<S>(el);
        }
    }
}
