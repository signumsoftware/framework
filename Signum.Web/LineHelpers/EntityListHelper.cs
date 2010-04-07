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
        private static void InternalEntityList<T>(this HtmlHelper helper, TypeContext<MList<T>> typeContext, EntityList settings)
        {
            if (!settings.Visible || settings.HideIfNull && typeContext.Value == null)
                return;
            
            string prefix = helper.GlobalName(typeContext.Name);
            MList<T> value = typeContext.Value;         
            Type elementsCleanStaticType = Reflector.ExtractLite(typeof(T)) ?? typeof(T);

            long? ticks = EntityBaseHelper.GetTicks(helper, prefix, settings);

            StringBuilder sb = new StringBuilder();

            sb.AppendLine(helper.HiddenStaticInfo(prefix, new StaticInfo(elementsCleanStaticType) { IsReadOnly = settings.ReadOnly }));
            sb.AppendLine(helper.Hidden(TypeContext.Compose(prefix, TypeContext.Ticks), ticks.TryToString() ?? ""));
            
            sb.AppendLine(EntityBaseHelper.WriteImplementations(helper, settings, prefix));

            sb.AppendLine(EntityBaseHelper.WriteLabel(helper, prefix, settings));

            //If it's an embeddedEntity write an empty template with index 0 to be used when creating a new item
            if (typeof(EmbeddedEntity).IsAssignableFrom(elementsCleanStaticType))
                sb.AppendLine("<script type=\"text/javascript\">var {0} = \"{1}\";</script>".Formato(
                        TypeContext.Compose(prefix, EntityBaseKeys.Template),
                        EntityBaseHelper.JsEscape(ListBaseHelper.RenderItemPopupContents(helper, prefix, typeContext, (T)(object)Constructor.Construct(typeof(T)), 0, settings, elementsCleanStaticType, elementsCleanStaticType, typeof(Lite).IsAssignableFrom(typeof(T))))));

            if (settings.ShowFieldDiv)
                sb.AppendLine("<div class='fieldList'>");

            StringBuilder sbSelect = new StringBuilder();
            sbSelect.AppendLine("<select id='{0}' name='{0}' multiple='multiple' ondblclick=\"{1}\" class='entityList'>".Formato(prefix, settings.GetViewing()));

            if (value != null)
            {
                for (int i = 0; i < value.Count; i++)
                    sb.Append(InternalListElement(helper, sbSelect, prefix, value[i], i, settings, typeContext));
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

            helper.ViewContext.HttpContext.Response.Write(sb.ToString());
        }

        private static string InternalListElement<T>(this HtmlHelper helper, StringBuilder sbOptions, string idValueField, T value, int index, EntityList settings, TypeContext<MList<T>> typeContext)
        {
            string indexedPrefix = TypeContext.Compose(idValueField, index.ToString());
            Type cleanStaticType = Reflector.ExtractLite(typeof(T)) ?? typeof(T);
            bool isIdentifiable = typeof(IdentifiableEntity).IsAssignableFrom(typeof(T));
            bool isLite = typeof(Lite).IsAssignableFrom(typeof(T));
            
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
                    sb.AppendLine(ListBaseHelper.RenderItemPopupInEntityDiv(helper, indexedPrefix, typeContext, value, index, settings, cleanRuntimeType, cleanStaticType, isLite));

                else if (value != null)
                    sb.Append(helper.Div(TypeContext.Compose(indexedPrefix, EntityBaseKeys.Entity), "", "", new Dictionary<string, object> { { "style", "display:none" } }));

                //Note this is added to the sbOptions, not to the result sb
                sbOptions.AppendLine("<option id='{0}' name='{0}' value='' class='valueLine entityListOption'>".Formato(TypeContext.Compose(indexedPrefix, EntityBaseKeys.ToStr)) +
                                ((isIdentifiable)
                                    ? ((IdentifiableEntity)(object)value).TryCC(i => i.ToString())
                                    : ((Lite)(object)value).TryCC(i => i.ToStr)) +
                                "</option>");
            }
            else
            {
                //It's an embedded entity: Render popupcontrol with embedded entity to the _sfEntity hidden div
                //sb.AppendLine(helper.HiddenRuntimeInfo(indexedPrefix, new EmbeddedRuntimeInfo<T>(value, false) { Ticks = ticks }));

                sb.AppendLine(ListBaseHelper.RenderItemPopupInEntityDiv(helper, indexedPrefix, typeContext, value, index, settings, cleanRuntimeType, cleanStaticType, isLite));

                //Note this is added to the sbOptions, not to the result sb
                sbOptions.AppendLine("<option id='{0}' name='{0}' value='' class='valueLine entityListOption'>".Formato(TypeContext.Compose(indexedPrefix, EntityBaseKeys.ToStr)) +
                                ((EmbeddedEntity)(object)value).TryCC(i => i.ToString()) + 
                                "</option>");
            }

            //sb.AppendLine("<script type=\"text/javascript\">var " + TypeContext.Compose(indexedPrefix, EntityBaseKeys.EntityTemp) + " = '';</script>");

            return sb.ToString();
        }

        public static void EntityList<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property)
        {
            helper.EntityList<T, S>(tc, property, null);
        }

        public static void EntityList<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property, Action<EntityList> settingsModifier)
        {
            TypeContext<MList<S>> context = Common.WalkExpression(tc, property);

            EntityList el = new EntityList(helper.GlobalName(context.Name));
            Navigator.ConfigureEntityBase(el, Reflector.ExtractLite(typeof(S)) ?? typeof(S), false);
            Common.FireCommonTasks(el, context);

            if (settingsModifier != null)
                settingsModifier(el);

            using (el)
                helper.InternalEntityList<S>(context, el);
        }
    }
}
