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
        private static string InternalEntityRepeater<T>(this HtmlHelper helper, EntityRepeater entityRepeater)
        {
            if (!entityRepeater.Visible || entityRepeater.HideIfNull && entityRepeater.UntypedValue == null)
                return "";

            StringBuilder sb = new StringBuilder();

            sb.AppendLine(EntityBaseHelper.BaseLineLabel(helper, entityRepeater));

            sb.AppendLine(helper.Hidden(entityRepeater.Compose(EntityBaseKeys.StaticInfo), new StaticInfo(entityRepeater.ElementType.CleanType()) { IsReadOnly = entityRepeater.ReadOnly }.ToString(), new { disabled = "disabled" }).ToHtmlString());
            sb.AppendLine(helper.Hidden(entityRepeater.Compose(TypeContext.Ticks), EntityInfoHelper.GetTicks(helper, entityRepeater).TryToString() ?? "").ToHtmlString());

            sb.AppendLine(EntityBaseHelper.WriteImplementations(helper, entityRepeater));

            //If it's an embeddedEntity write an empty template with index 0 to be used when creating a new item
            if (entityRepeater.ElementType.IsEmbeddedEntity())
            {
                TypeElementContext<T> templateTC = new TypeElementContext<T>((T)(object)Constructor.Construct(typeof(T)), (TypeContext)entityRepeater.Parent, 0);
                sb.AppendLine(EntityBaseHelper.EmbeddedTemplate(entityRepeater, EntityBaseHelper.RenderTypeContext(helper, templateTC, RenderMode.Content, entityRepeater)));
            } 
            
            sb.AppendLine(ListBaseHelper.WriteCreateButton(helper, entityRepeater, new Dictionary<string, object>{{"title", entityRepeater.AddElementLinkText}}));
            sb.AppendLine(ListBaseHelper.WriteFindButton(helper, entityRepeater));

            sb.AppendLine(helper.Div("", "", "clearall", null)); //To keep create and find buttons' space

            sb.AppendLine("<div id='{0}' name='{0}'>".Formato(entityRepeater.Compose(EntityRepeaterKeys.ItemsContainer)));
            if (entityRepeater.UntypedValue != null)
            {
                foreach (var itemTC in TypeContextUtilities.TypeElementContext((TypeContext<MList<T>>)entityRepeater.Parent))
                    sb.Append(InternalRepeaterElement(helper, itemTC, entityRepeater));
            }
            sb.AppendLine("</div>");

            sb.AppendLine(EntityBaseHelper.WriteBreakLine(helper, entityRepeater));

            return sb.ToString();
        }

        private static string InternalRepeaterElement<T>(this HtmlHelper helper, TypeElementContext<T> itemTC, EntityRepeater entityRepeater)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<div id='{0}' name='{0}' class='repeaterElement'>".Formato(itemTC.Compose(EntityRepeaterKeys.RepeaterElement)));

            if (!entityRepeater.ForceNewInUI)
                sb.AppendLine(helper.Hidden(itemTC.Compose(EntityListBaseKeys.Index), itemTC.Index.ToString()).ToHtmlString());

            sb.AppendLine(helper.HiddenRuntimeInfo(itemTC));

            if (entityRepeater.Remove)
                sb.AppendLine(
                    helper.Href(itemTC.Compose("btnRemove"),
                                entityRepeater.RemoveElementLinkText,
                                "javascript:ERepOnRemoving({0}, '{1}');".Formato(entityRepeater.ToJS(), itemTC.ControlID),
                                entityRepeater.RemoveElementLinkText,
                                "lineButton remove", 
                                null));

            sb.AppendLine(helper.Div("", "", "clearall", null)); //To keep remove button space

            sb.AppendLine(EntityBaseHelper.RenderTypeContext(helper, itemTC, RenderMode.ContentInVisibleDiv, entityRepeater));

            sb.AppendLine("</div>");

            return sb.ToString();
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
