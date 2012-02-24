#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Operations;
using System.Web.Mvc;
using Signum.Entities;
using System.Web;
using Signum.Utilities;
using Signum.Entities.Basics;
using Signum.Web.Extensions.Properties;
#endregion

namespace Signum.Web.Operations
{
    public static class OperationButtonFactory
    {
        public static ToolBarButton Create(EntityOperationContext ctx)
        {
            return new ToolBarButton
            {
                Id = EnumDN.UniqueKey(ctx.OperationInfo.Key),

                DivCssClass = " ".CombineIfNotEmpty(
                    ToolBarButton.DefaultEntityDivCssClass,
                    EntityOperationSettings.CssClass(ctx.OperationInfo.Key)),

                AltText = ctx.OperationSettings.TryCC(o => o.AltText) ?? ctx.OperationInfo.CanExecute,
                Enabled = ctx.OperationInfo.CanExecute == null,

                Text = ctx.OperationSettings.TryCC(o => o.Text) ?? ctx.OperationInfo.Key.NiceToString(),
                OnClick = ((ctx.OperationSettings != null && ctx.OperationSettings.OnClick != null) ? ctx.OperationSettings.OnClick(ctx) : DefaultClick(ctx)).ToJS(),
            };
        }

        public static ContextualItem Create(ContextualOperationContext ctx, EntityOperationContext entityCtx)
        {
            return new ContextualItem
            {
                Id = EnumDN.UniqueKey(ctx.OperationInfo.Key),

                DivCssClass = " ".CombineIfNotEmpty(
                    ToolBarButton.DefaultEntityDivCssClass,
                    EntityOperationSettings.CssClass(ctx.OperationInfo.Key)),

                AltText = ctx.OperationSettings.TryCC(o => o.AltText) ?? ctx.OperationInfo.CanExecute,
                Enabled = ctx.OperationInfo.CanExecute == null,

                Text = ctx.OperationSettings.TryCC(o => o.Text) ?? ctx.OperationInfo.Key.NiceToString(),
                OnClick = (ctx.OperationSettings != null && ctx.OperationSettings.OnContextualClick != null) ? ctx.OperationSettings.OnContextualClick(ctx).ToJS()
                        : (entityCtx.OperationSettings != null && entityCtx.OperationSettings.OnClick != null) ? entityCtx.OperationSettings.OnClick(entityCtx).ToJS()
                        : DefaultContextualClick(ctx).ToJS()
            };
        }

        public static ToolBarButton Create(QueryOperationContext ctx)
        {
            return new ToolBarButton
            {
                Id = EnumDN.UniqueKey(ctx.OperationInfo.Key),

                DivCssClass = " ".CombineIfNotEmpty(
                    ToolBarButton.DefaultQueryCssClass,
                    EntityOperationSettings.CssClass(ctx.OperationInfo.Key)),

                AltText = ctx.OperationSettings.TryCC(o => o.AltText),

                Text = ctx.OperationSettings.TryCC(o => o.Text) ?? ctx.OperationInfo.Key.NiceToString(),
                OnClick = (ctx.OperationSettings != null && ctx.OperationSettings.OnClick != null ? ctx.OperationSettings.OnClick(ctx) : DefaultClick(ctx)).ToJS(),
            };
        }

        static JsInstruction DefaultClick(EntityOperationContext ctx)
        {
            switch (ctx.OperationInfo.OperationType)
            {
                case OperationType.Execute:
                    return new JsOperationExecutor(ctx.Options()).validateAndAjax();
                case OperationType.Delete:
                    return new JsOperationDelete(ctx.Options()).confirmAndAjax(ctx.Entity);
                case OperationType.ConstructorFrom:
                    return new JsOperationConstructorFrom(ctx.Options()).validateAndAjax();                    
                default:
                    throw new InvalidOperationException("Invalid Operation Type '{0}' in the construction of the operation '{1}'".Formato(ctx.OperationInfo.OperationType.ToString(), EnumDN.UniqueKey(ctx.OperationInfo.Key)));
            }
        }

        static JsInstruction DefaultContextualClick(ContextualOperationContext ctx)
        {
            switch (ctx.OperationInfo.OperationType)
            {
                case OperationType.Execute:
                    return new JsOperationExecutor(ctx.Options()).ContextualExecute(ctx.Entity, ctx.OperationSettings.TryCC(o => o.Text) ?? ctx.OperationInfo.Key.NiceToString());
                case OperationType.Delete:
                    return new JsOperationDelete(ctx.Options()).ContextualDelete(ctx.Entity);
                case OperationType.ConstructorFrom:
                    return new JsOperationConstructorFrom(ctx.Options()).ContextualConstruct(ctx.Entity, ctx.OperationSettings.TryCC(o => o.Text) ?? ctx.OperationInfo.Key.NiceToString());
                default:
                    throw new InvalidOperationException("Invalid Operation Type '{0}' in the construction of the operation '{1}'".Formato(ctx.OperationInfo.OperationType.ToString(), EnumDN.UniqueKey(ctx.OperationInfo.Key)));
            }
        }

        static JsInstruction DefaultClick(QueryOperationContext ctx)
        {
            if (ctx.OperationInfo.OperationType != OperationType.ConstructorFromMany)
                throw new InvalidOperationException("Invalid Operation Type '{0}' in the construction of the operation '{1}'".Formato(ctx.OperationInfo.OperationType.ToString(), EnumDN.UniqueKey(ctx.OperationInfo.Key)));

            return new JsOperationConstructorFromMany(ctx.Options()).ajaxSelected(Js.NewPrefix(ctx.Prefix), JsOpSuccess.DefaultDispatcher);
        }
    }
}
