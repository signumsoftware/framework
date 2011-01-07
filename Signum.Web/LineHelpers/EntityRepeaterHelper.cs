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
using Signum.Engine;
#endregion

namespace Signum.Web
{
    public static class EntityRepeaterHelper
    {
        private static MvcHtmlString InternalEntityRepeater<T>(this HtmlHelper helper, EntityRepeater entityRepeater)
        {
            if (!entityRepeater.Visible || entityRepeater.HideIfNull && entityRepeater.UntypedValue == null)
                return MvcHtmlString.Empty;

            HtmlStringBuilder sb = new HtmlStringBuilder();

            sb.AddLine(EntityBaseHelper.BaseLineLabel(helper, entityRepeater));

            sb.AddLine(helper.HiddenStaticInfo(entityRepeater));
            sb.AddLine(helper.Hidden(entityRepeater.Compose(TypeContext.Ticks), EntityInfoHelper.GetTicks(helper, entityRepeater).TryToString() ?? ""));

            //If it's an embeddedEntity write an empty template with index 0 to be used when creating a new item
            if (entityRepeater.ElementType.IsEmbeddedEntity())
            {
                TypeElementContext<T> templateTC = new TypeElementContext<T>((T)(object)Constructor.Construct(typeof(T)), (TypeContext)entityRepeater.Parent, 0);
                sb.AddLine(EntityBaseHelper.EmbeddedTemplate(entityRepeater, EntityBaseHelper.RenderTypeContext(helper, templateTC, RenderMode.Content, entityRepeater)));
            }

            sb.AddLine(ListBaseHelper.CreateButton(helper, entityRepeater, new Dictionary<string, object> { { "title", entityRepeater.AddElementLinkText } }));
            sb.AddLine(ListBaseHelper.FindButton(helper, entityRepeater));

            sb.AddLine(helper.Div("", null,"clearall", null)); //To keep create and find buttons' space

            using (sb.Surround(new HtmlTag("div").IdName(entityRepeater.Compose(EntityRepeaterKeys.ItemsContainer))))
            {
                if (entityRepeater.UntypedValue != null)
                {
                    foreach (var itemTC in TypeContextUtilities.TypeElementContext((TypeContext<MList<T>>)entityRepeater.Parent))
                        sb.Add(InternalRepeaterElement(helper, itemTC, entityRepeater));
                }
            }

            sb.AddLine(EntityBaseHelper.BreakLineDiv(helper, entityRepeater));

            return sb.ToHtml();
        }

        private static MvcHtmlString InternalRepeaterElement<T>(this HtmlHelper helper, TypeElementContext<T> itemTC, EntityRepeater entityRepeater)
        {
            HtmlStringBuilder sb = new HtmlStringBuilder();

            using (sb.Surround(new HtmlTag("div").IdName(itemTC.Compose(EntityRepeaterKeys.RepeaterElement)).Class("repeaterElement")))
            {
                if (!entityRepeater.ForceNewInUI)
                    sb.AddLine(helper.Hidden(itemTC.Compose(EntityListBaseKeys.Index), itemTC.Index.ToString()));

                sb.AddLine(helper.HiddenRuntimeInfo(itemTC));

                if (entityRepeater.Remove)
                    sb.AddLine(
                        helper.Href(itemTC.Compose("btnRemove"),
                                    entityRepeater.RemoveElementLinkText,
                                    "javascript:ERepOnRemoving({0}, '{1}');".Formato(entityRepeater.ToJS(), itemTC.ControlID),
                                    entityRepeater.RemoveElementLinkText,
                                    "lineButton remove",
                                    null));

                sb.AddLine(helper.Div("", null, "clearall", null)); //To keep remove button space

                sb.AddLine(EntityBaseHelper.RenderTypeContext(helper, itemTC, RenderMode.ContentInVisibleDiv, entityRepeater));
            }

            return sb.ToHtml();
        }

        public static void EntityRepeater<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property)
            where S : Modifiable 
        {
            helper.EntityRepeater(tc, property, null);
        }

        public static void EntityRepeater<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property, Action<EntityRepeater> settingsModifier)
        {
            TypeContext<MList<S>> context = Common.WalkExpression(tc, property);

            EntityRepeater el = new EntityRepeater(context.Type, context.UntypedValue, context, null, context.PropertyRoute);

            EntityBaseHelper.ConfigureEntityBase(el, Reflector.ExtractLite(typeof(S)) ?? typeof(S));

            Common.FireCommonTasks(el);

            if (settingsModifier != null)
                settingsModifier(el);

            helper.Write(helper.InternalEntityRepeater<S>(el));
        }
    }
}
