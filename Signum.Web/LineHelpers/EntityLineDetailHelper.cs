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
            using (sb.Surround(new HtmlTag("fieldset", entityDetail.Prefix).Class("SF-entity-line-details SF-control-container")))
            {
                sb.AddLine(helper.HiddenRuntimeInfo(entityDetail));

                using (sb.Surround(new HtmlTag("div", entityDetail.Compose("hidden")).Class("hide")))
                {
                    if (entityDetail.UntypedValue != null)
                    {
                        sb.AddLine(EntityListBaseHelper.CreateSpan(helper, entityDetail));
                        sb.AddLine(EntityListBaseHelper.FindSpan(helper, entityDetail));
                    }
                    else
                    {
                        sb.AddLine(EntityListBaseHelper.RemoveSpan(helper, entityDetail));
                    }
                }

                using (sb.Surround(new HtmlTag("legend")))
                using (sb.Surround(new HtmlTag("div", entityDetail.Compose("header"))))
                {
                    sb.AddLine(new HtmlTag("span").SetInnerText(entityDetail.LabelText).ToHtml());

                    using (sb.Surround(new HtmlTag("span", entityDetail.Compose("shownButton")).Class("pull-right")))
                    {
                        if (entityDetail.UntypedValue == null)
                        {
                            sb.AddLine(EntityListBaseHelper.CreateSpan(helper, entityDetail));
                            sb.AddLine(EntityListBaseHelper.FindSpan(helper, entityDetail));
                        }
                        else
                        {
                            sb.AddLine(EntityListBaseHelper.RemoveSpan(helper, entityDetail));
                        }
                    }
                }

                using (sb.Surround(new HtmlTag("div", entityDetail.Compose(EntityBaseKeys.Detail))))
                {
                    if (entityDetail.UntypedValue != null)
                        sb.AddLine(EntityBaseHelper.RenderContent(helper, (TypeContext)entityDetail.Parent, RenderContentMode.Content, entityDetail));
                }

                if (entityDetail.Type.IsEmbeddedEntity())
                {
                    TypeContext templateTC = ((TypeContext)entityDetail.Parent).Clone((object)Constructor.Construct(entityDetail.Type.CleanType()));
                    sb.AddLine(EntityBaseHelper.EmbeddedTemplate(entityDetail, EntityBaseHelper.RenderContent(helper, templateTC, RenderContentMode.Content, entityDetail), null));
                }

                sb.AddLine(entityDetail.ConstructorScript(JsFunction.LinesModule, "EntityLineDetail"));
            }

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
