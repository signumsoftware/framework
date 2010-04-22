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
                DivCssClass = ctx.OperationSettings.TryCC(o => o.CssClass) ?? ToolBarButton.DefaultEntityDivCssClass,
                AltText = ctx.OperationSettings.TryCC(o => o.AltText) ?? ctx.OperationInfo.CanExecute,
                Enabled = ctx.OperationInfo.CanExecute == null,

                Text = ctx.OperationSettings.TryCC(o => o.Text) ?? ctx.OperationInfo.Key.NiceToString(),
                OnClick = (ctx.OperationSettings != null && ctx.OperationSettings.OnClick != null ? ctx.OperationSettings.OnClick(ctx) : DefaultClick(ctx)).ToJS(),
            };
        }

        public static ToolBarButton Create(QueryOperationContext ctx)
        {
            return new ToolBarButton
            {
                Id = EnumDN.UniqueKey(ctx.OperationInfo.Key),
                DivCssClass = ctx.OperationSettings.TryCC(o => o.CssClass) ?? ToolBarButton.DefaultQueryDivCssClass,
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
                    return new JsOperationExecutor(ctx.Options()).DefaultExecute();
                case OperationType.Delete:
                    return new JsOperationDelete(ctx.Options()).DefaultDelete();
                case OperationType.ConstructorFrom:
                    return new JsOperationConstructorFrom(ctx.Options()).DefaultConstruct();                    
                default:
                    throw new InvalidOperationException(Resources.InvalidOperationType0inTheConstructionOfOperation1.Formato(ctx.OperationInfo.OperationType.ToString(), EnumDN.UniqueKey(ctx.OperationInfo.Key)));
            }
        }

        static JsInstruction DefaultClick(QueryOperationContext ctx)
        {
            if (ctx.OperationInfo.OperationType != OperationType.ConstructorFromMany)
                throw new InvalidOperationException(Resources.InvalidOperationType0inTheConstructionOfOperation1.Formato(ctx.OperationInfo.OperationType.ToString(), EnumDN.UniqueKey(ctx.OperationInfo.Key)));

            return new JsOperationConstructorFromMany(ctx.Options()).DefaultConstruct();
        }
    }
}
