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
        private static void InternalEntityListDetail<T>(this HtmlHelper helper, TypeContext<MList<T>> typeContext, EntityListDetail settings)
        {
            if (!settings.Visible || settings.HideIfNull && typeContext.Value == null)
                return;
            
            string prefix = helper.GlobalName(typeContext.Name);
            MList<T> value = typeContext.Value;         
            Type elementsCleanStaticType = Reflector.ExtractLite(typeof(T)) ?? typeof(T);

            long? ticks = EntityBaseHelper.GetTicks(helper, prefix, settings);
            
            string defaultDetailDiv = TypeContext.Compose(prefix, EntityBaseKeys.Detail);
            if (!settings.DetailDiv.HasText())
                settings.DetailDiv = defaultDetailDiv;

            StringBuilder sb = new StringBuilder();

            sb.AppendLine(helper.HiddenEntityInfo(prefix, new RuntimeInfo { Ticks = ticks }, new StaticInfo(elementsCleanStaticType) { IsReadOnly = settings.ReadOnly }));

            sb.AppendLine(EntityBaseHelper.WriteImplementations(helper, settings, prefix));

            sb.AppendLine(EntityBaseHelper.WriteLabel(helper, prefix, settings));

            //If it's an embeddedEntity write an empty template with index 0 to be used when creating a new item
            if (typeof(EmbeddedEntity).IsAssignableFrom(elementsCleanStaticType))
                sb.AppendLine("<script type=\"text/javascript\">var {0} = \"{1}\";</script>".Formato(
                        TypeContext.Compose(prefix, EntityBaseKeys.Template),
                        EntityBaseHelper.JsEscape(ListBaseHelper.RenderItemContent(helper, prefix, typeContext, (T)(object)Constructor.ConstructStrict(typeof(T)), 0, settings, elementsCleanStaticType, elementsCleanStaticType, typeof(Lite).IsAssignableFrom(typeof(T))))));

            if (settings.ShowFieldDiv)
                sb.AppendLine("<div class='fieldList'>");

            StringBuilder sbSelect = new StringBuilder();
            sbSelect.AppendLine("<select id='{0}' name='{0}' multiple='multiple' ondblclick=\"{1}\" class='entityList'>".Formato(prefix, settings.GetViewing()));
            if (value != null)
            {
                for (int i = 0; i < value.Count; i++)
                    sb.Append(InternalListDetailElement(helper, sbSelect, prefix, value[i], i, settings, typeContext));
            }
            sbSelect.AppendLine("</select>");

            sb.Append(sbSelect);

            StringBuilder sbBtns = new StringBuilder();
            sbBtns.AppendLine("<tr><td>" + ListBaseHelper.WriteCreateButton(helper, settings, null) + "</td></tr>");
            sbBtns.AppendLine("<tr><td>" + ListBaseHelper.WriteFindButton(helper, settings, elementsCleanStaticType) + "</td></tr>");
            sbBtns.AppendLine("<tr><td>" + ListBaseHelper.WriteRemoveButton(helper, settings, value) + "</td></tr>");

            string sBtns = sbBtns.ToString();
            if (sBtns.HasText())
                sb.AppendLine("<table>\n" + sBtns + "</table>");

            if (settings.ShowFieldDiv)
                sb.Append("</div>");

            sb.AppendLine(EntityBaseHelper.WriteBreakLine());

            if (settings.DetailDiv == defaultDetailDiv)
                sb.AppendLine("<div id='{0}' name='{0}'>{1}</div>".Formato(settings.DetailDiv, ""));
            
            if (value != null && value.Count > 0)
                sb.AppendLine("<script type=\"text/javascript\">\n" +
                        "$(document).ready(function() {" +
                        "$('#" + prefix + "').dblclick();\n" +
                        "});" +
                        "</script>");

            sb.AppendLine(EntityBaseHelper.WriteBreakLine());

            helper.ViewContext.HttpContext.Response.Write(sb.ToString());
        }

        private static string InternalListDetailElement<T>(this HtmlHelper helper, StringBuilder sbOptions, string idValueField, T value, int index, EntityListDetail settings, TypeContext<MList<T>> typeContext)
        {
            bool isIdentifiable = typeof(IdentifiableEntity).IsAssignableFrom(typeof(T));
            bool isLite = typeof(Lite).IsAssignableFrom(typeof(T));
            string indexedPrefix = TypeContext.Compose(idValueField, index.ToString());
            Type cleanStaticType = Reflector.ExtractLite(typeof(T)) ?? typeof(T);
                        
            Type cleanRuntimeType = null;
            if (value != null)
                cleanRuntimeType = typeof(Lite).IsAssignableFrom(value.GetType()) ? (value as Lite).RuntimeType : value.GetType();

            long? ticks = EntityBaseHelper.GetTicks(helper, indexedPrefix, settings);

            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine(helper.Hidden(TypeContext.Compose(indexedPrefix, EntityListBaseKeys.Index), index.ToString()));

            sb.AppendLine(helper.HiddenRuntimeInfo(indexedPrefix, new RuntimeInfo(value) { Ticks = ticks }));

            if (isIdentifiable || isLite)
            {
                //sb.AppendLine(helper.HiddenRuntimeInfo(indexedPrefix, new RuntimeInfo<T>(value) { Ticks = ticks }));

                if (EntityBaseHelper.RequiresLoadAll(helper, isIdentifiable, isLite, value, indexedPrefix))
                    sb.AppendLine(ListBaseHelper.RenderItemContentInEntityDiv(helper, indexedPrefix, typeContext, value, index, settings, cleanRuntimeType, cleanStaticType, isLite, false));

                else if (value != null)
                    sb.Append(helper.Div(TypeContext.Compose(indexedPrefix, EntityBaseKeys.Entity), "", "", new Dictionary<string, object> { { "style", "display:none" } }));

                //Note this is added to the sbOptions, not to the result sb
                sbOptions.AppendLine("<option id='{0}' name='{0}' value='' class='valueLine entityListOption'{1}>".Formato(TypeContext.Compose(indexedPrefix, EntityBaseKeys.ToStr), (index == 0) ? "selected='selected'" : "") +
                                ((isIdentifiable)
                                    ? ((IdentifiableEntity)(object)value).TryCC(i => i.ToString())
                                    : ((Lite)(object)value).TryCC(i => i.ToStr)) +
                                "</option>");
            }
            else
            {
                //It's an embedded entity: Render popupcontrol with embedded entity to the _sfEntity hidden div
                //sb.AppendLine(helper.HiddenRuntimeInfo(indexedPrefix, new EmbeddedRuntimeInfo<T>(value, false) { Ticks = ticks }));

                sb.AppendLine(ListBaseHelper.RenderItemContentInEntityDiv(helper, indexedPrefix, typeContext, value, index, settings, cleanRuntimeType, cleanStaticType, isLite, false));

                //Note this is added to the sbOptions, not to the result sb
                sbOptions.AppendLine("<option id='{0}' name='{0}' value='' class='valueLine entityListOption'>".Formato(TypeContext.Compose(indexedPrefix, EntityBaseKeys.ToStr)) +
                                ((EmbeddedEntity)(object)value).TryCC(i => i.ToString()) + 
                                "</option>");
            }

            return sb.ToString();
        }

        public static void EntityListDetail<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property)
        {
            helper.EntityListDetail<T, S>(tc, property, null);
        }

        public static void EntityListDetail<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property, Action<EntityListDetail> settingsModifier)
        {
            TypeContext<MList<S>> context = Common.WalkExpression(tc, property);

            EntityListDetail el = new EntityListDetail(helper.GlobalName(context.Name));
            Navigator.ConfigureEntityBase(el, Reflector.ExtractLite(typeof(S)) ?? typeof(S), false);
            Common.FireCommonTasks(el, context);

            if (settingsModifier != null)
                settingsModifier(el);

            using (el)
                helper.InternalEntityListDetail<S>(context, el);
        }
    }
}
