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
#endregion

namespace Signum.Web
{
    public static class EntityLineDetailHelper
    {
        internal static MvcHtmlString InternalEntityLineDetail(this HtmlHelper helper, EntityLineDetail entityDetail)
        {
            if (!entityDetail.Visible || entityDetail.HideIfNull && entityDetail.UntypedValue == null)
                return MvcHtmlString.Empty;

            HtmlStringBuilder sb = new HtmlStringBuilder();

            using (sb.Surround(new HtmlTag("fieldset").Id(entityDetail.ControlID).Class("sf-line-detail-field SF-control-container")))
            {
                using (sb.Surround(new HtmlTag("legend")))
                {
                    sb.AddLine(EntityBaseHelper.BaseLineLabel(helper, entityDetail));

                    sb.AddLine(EntityBaseHelper.CreateButton(helper, entityDetail, hidden: entityDetail.UntypedValue != null));
                    sb.AddLine(EntityBaseHelper.FindButton(helper, entityDetail, hidden: entityDetail.UntypedValue != null));
                    sb.AddLine(EntityBaseHelper.RemoveButton(helper, entityDetail, hidden: entityDetail.UntypedValue == null));
                }

                sb.AddLine(helper.HiddenEntityInfo(entityDetail));

                if (entityDetail.Type.IsEmbeddedEntity())
                {
                    TypeContext templateTC = ((TypeContext)entityDetail.Parent).Clone((object)Constructor.Construct(entityDetail.Type.CleanType()));
                    sb.AddLine(EntityBaseHelper.EmbeddedTemplate(entityDetail, EntityBaseHelper.RenderContent(helper, templateTC, RenderContentMode.Content, entityDetail)));
                }

                MvcHtmlString controlHtml = null;
                if (entityDetail.UntypedValue != null)
                    controlHtml = EntityBaseHelper.RenderContent(helper, (TypeContext)entityDetail.Parent, RenderContentMode.Content, entityDetail);

                if (entityDetail.DetailDiv == entityDetail.DefaultDetailDiv)
                    sb.AddLine(helper.Div(entityDetail.DetailDiv, controlHtml, "sf-entity-line-detail"));
                else if (controlHtml != null)
                    sb.AddLine(MvcHtmlString.Create("<script type=\"text/javascript\">\n" +
                            "$(document).ready(function() {\n" +
                            "$('#" + entityDetail.DetailDiv + "').html(" + controlHtml + ");\n" +
                            "});\n" +
                            "</script>"));
            }

            sb.AddLine(entityDetail.ConstructorScript(JsFunction.LinesModule, "EntityLineDetail"));

            return sb.ToHtml();
        }

        public static MvcHtmlString EntityLineDetail<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property) 
        {
            return helper.EntityLineDetail<T, S>(tc, property, null);
        }

        public static MvcHtmlString EntityLineDetail<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<EntityLineDetail> settingsModifier)
        {
            TypeContext<S> context = Common.WalkExpression(tc, property);

            EntityLineDetail eld = new EntityLineDetail(context.Type, context.Value, context, null, context.PropertyRoute); 
           
            EntityBaseHelper.ConfigureEntityBase(eld, eld.CleanRuntimeType ?? eld.Type.CleanType());

            Common.FireCommonTasks(eld);

            if (settingsModifier != null)
                settingsModifier(eld);

            var result = helper.InternalEntityLineDetail(eld);

            var vo = eld.ViewOverrides;
            if (vo == null)
                return result;

            return vo.OnSurroundLine(eld.PropertyRoute, helper, tc, result);
        }
    }
}
