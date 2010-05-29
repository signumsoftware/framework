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
using Signum.Web.Properties;
#endregion

namespace Signum.Web
{
    public static class EntityLineDetailHelper
    {
        internal static string InternalEntityLineDetail(this HtmlHelper helper, EntityLineDetail entityDetail)
        {
            if (!entityDetail.Visible || entityDetail.HideIfNull && entityDetail.UntypedValue == null)
                return "";

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<div class='EntityLineDetail'>");

            sb.AppendLine(EntityBaseHelper.BaseLineLabel(helper, entityDetail));

            sb.AppendLine(helper.HiddenEntityInfo(entityDetail));

            if (entityDetail.Type.IsIIdentifiable() || entityDetail.Type.IsLite())
                sb.AppendLine(EntityBaseHelper.WriteImplementations(helper, entityDetail));
            else
            {
                TypeContext templateTC = ((TypeContext)entityDetail.Parent).Clone((object)Constructor.Construct(entityDetail.Type.CleanType()));
                sb.AppendLine(EntityBaseHelper.EmbeddedTemplate(entityDetail, EntityBaseHelper.RenderTypeContext(helper, templateTC, RenderMode.Content, entityDetail.PartialViewName, entityDetail.ReloadOnChange)));
            }

            sb.AppendLine(EntityBaseHelper.WriteCreateButton(helper, entityDetail));
            sb.AppendLine(EntityBaseHelper.WriteFindButton(helper, entityDetail));
            sb.AppendLine(EntityBaseHelper.WriteRemoveButton(helper, entityDetail));

            sb.AppendLine(EntityBaseHelper.WriteBreakLine(helper, entityDetail));

            string controlHtml = null;
            if (entityDetail.UntypedValue != null)
                controlHtml = EntityBaseHelper.RenderTypeContext(helper, (TypeContext)entityDetail.Parent, RenderMode.Content, entityDetail.PartialViewName, entityDetail.ReloadOnChange);
            
            if (entityDetail.DetailDiv == entityDetail.DefaultDetailDiv)
                sb.AppendLine(helper.Div(entityDetail.DetailDiv, controlHtml ?? "", ""));
            else if (controlHtml != null)
                sb.AppendLine("<script type=\"text/javascript\">\n" +
                        "$(document).ready(function() {\n" +
                        "$('#" + entityDetail.DetailDiv + "').html(" + controlHtml + ");\n" +
                        "});\n" +
                        "</script>");

            sb.AppendLine("</div>"); //Closing tag of <div class='EntityLineDetail'>

            return sb.ToString();
        }

        public static void EntityLineDetail<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property) 
        {
            helper.EntityLineDetail<T, S>(tc, property, null);
        }

        public static void EntityLineDetail<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<EntityLineDetail> settingsModifier)
        {
            TypeContext<S> context = Common.WalkExpression(tc, property);

            EntityLineDetail edl = new EntityLineDetail(context.Type, context.Value, context, null, context.PropertyRoute); 
           
            Common.FireCommonTasks(edl);

            EntityBaseHelper.ConfigureEntityBase(edl, edl.CleanRuntimeType ?? edl.Type.CleanType());

            if (settingsModifier != null)
                settingsModifier(edl);

            helper.Write(helper.InternalEntityLineDetail(edl));
        }
    }
}
