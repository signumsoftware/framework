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

namespace Signum.Web
{
    public static class EntityDetailHelper
    {
        internal static MvcHtmlString InternalEntityDetail(this HtmlHelper helper, EntityDetail entityDetail)
        {
            if (!entityDetail.Visible || entityDetail.HideIfNull && entityDetail.UntypedValue == null)
                return MvcHtmlString.Empty;

            HtmlStringBuilder sb = new HtmlStringBuilder();
            using (sb.SurroundLine(new HtmlTag("fieldset", entityDetail.Prefix).Class("SF-entity-line-details SF-control-container")))
            {
                sb.AddLine(helper.HiddenRuntimeInfo(entityDetail));

                using (sb.SurroundLine(new HtmlTag("div", entityDetail.Compose("hidden")).Class("hide")))
                {
                    if (entityDetail.UntypedValue != null)
                    {
                        sb.AddLine(EntityButtonHelper.Create(helper, entityDetail, btn: false));
                        sb.AddLine(EntityButtonHelper.Find(helper, entityDetail, btn: false));
                    }
                    else
                    {
                        sb.AddLine(EntityButtonHelper.Remove(helper, entityDetail, btn: false));
                    }
                }

                using (sb.SurroundLine(new HtmlTag("legend")))
                using (sb.SurroundLine(new HtmlTag("div", entityDetail.Compose("header"))))
                {
                    sb.AddLine(new HtmlTag("span").InnerHtml(entityDetail.LabelHtml ?? entityDetail.LabelText.FormatHtml()).ToHtml());

                    using (sb.SurroundLine(new HtmlTag("span", entityDetail.Compose("shownButton")).Class("pull-right")))
                    {
                        if (entityDetail.UntypedValue == null)
                        {
                            sb.AddLine(EntityButtonHelper.Create(helper, entityDetail, btn: false));
                            sb.AddLine(EntityButtonHelper.Find(helper, entityDetail, btn: false));
                        }
                        else
                        {
                            sb.AddLine(EntityButtonHelper.Remove(helper, entityDetail, btn: false));
                        }
                    }
                }

                using (sb.SurroundLine(new HtmlTag("div", entityDetail.Compose(EntityBaseKeys.Detail))))
                {
                    if (entityDetail.UntypedValue != null)
                        sb.AddLine(EntityBaseHelper.RenderContent(helper, (TypeContext)entityDetail.Parent, RenderContentMode.Content, entityDetail));
                }

                if (entityDetail.Type.IsEmbeddedEntity() && entityDetail.Create)
                {
                    EmbeddedEntity embedded = (EmbeddedEntity)new ConstructorContext(helper.ViewContext.Controller).ConstructUntyped(entityDetail.Type.CleanType());
                    TypeContext templateTC = ((TypeContext)entityDetail.Parent).Clone(embedded);
                    sb.AddLine(EntityBaseHelper.EmbeddedTemplate(entityDetail, EntityBaseHelper.RenderContent(helper, templateTC, RenderContentMode.Content, entityDetail), null));
                }

                sb.AddLine(entityDetail.ConstructorScript(JsModule.Lines, "EntityDetail"));
            }

            return sb.ToHtml();
        }

        public static MvcHtmlString EntityDetail<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property) 
        {
            return helper.EntityDetail<T, S>(tc, property, null);
        }

        public static MvcHtmlString EntityDetail<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<EntityDetail> settingsModifier)
        {
            TypeContext<S> context = Common.WalkExpression(tc, property);

            var vo = tc.ViewOverrides;

            if (vo != null && !vo.IsVisible(context.PropertyRoute))
                return vo.OnSurroundLine(context.PropertyRoute, helper, tc, null);

            EntityDetail eld = new EntityDetail(context.Type, context.Value, context, null, context.PropertyRoute); 
           
            EntityBaseHelper.ConfigureEntityBase(eld, eld.CleanRuntimeType ?? eld.Type.CleanType());

            Common.FireCommonTasks(eld);

            if (settingsModifier != null)
                settingsModifier(eld);

            var result = helper.InternalEntityDetail(eld);

            if (vo == null)
                return result;

            return vo.OnSurroundLine(eld.PropertyRoute, helper, tc, result);
        }
    }
}
