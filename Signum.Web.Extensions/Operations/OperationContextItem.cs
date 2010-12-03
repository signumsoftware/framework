using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Utilities;
using System.Web.Mvc;
using Signum.Engine.Operations;
using Signum.Entities.Operations;
using System.Text;
using Signum.Entities;
using Signum.Engine;
using Signum.Web.Controllers;
using Signum.Web.Extensions.Properties;

namespace Signum.Web.Operations
{
    public class OperationsContextualItem : ContextualItem
    {
        public OperationsContextualItem()
        {
            DivCssClass = "operationsCtxItem";
        }

        public override string ToString()
        {
            return Content;
        }
    }

    public delegate OperationsContextualItem[] GetOperationsContextualItemDelegate(Lite lite);

    public static class OperationsContextualItemsHelper
    {
        static Dictionary<Type, List<Delegate>> contextualItems = new Dictionary<Type, List<Delegate>>();

        public static void RegisterContextualItems<T>(GetOperationsContextualItemDelegate getContextualItems)
        {
            contextualItems.GetOrCreate(typeof(T)).Add(getContextualItems);
        }

        public static void Start()
        {
            ContextualItemsHelper.GetContextualItemsForLite += CreateContextualItem;
        }

        public static ContextualItem CreateContextualItem(ControllerContext controllerContext, Lite lite, object queryName, string prefix)
        {
            List<OperationsContextualItem> operations = GetForLite(lite, queryName, prefix);
            if (operations == null || operations.Count == 0) return null;

            return new OperationsContextualItem
            {
                //Label = "<span id='{0}'>{0}</span>".Formato("Operations"),
                Id = "Operations",
                Content = @"<div class='contextualItem operations'><ul class='operation-ctx-menu'>{0}</ul></div>".Formato(
                    operations.ToString(ctx => "<li>" + ctx.IndividualOperationToString() + "</li>", "")),
            };
        }

        private static MvcHtmlString IndividualOperationToString(this OperationsContextualItem oci)
        {
            if (oci.ImgSrc.HasText())
            {
                if (oci.HtmlProps.ContainsKey("style"))
                    oci.HtmlProps["style"] = "background:transparent url(" + oci.ImgSrc + ")  no-repeat scroll -4px top; text-indent:12px; " + oci.HtmlProps["style"].ToString();
                else
                    oci.HtmlProps["style"] = "background:transparent url(" + oci.ImgSrc + ")  no-repeat scroll -4px top; text-indent:12px;";
            }

            if (oci.Enabled)
                oci.HtmlProps.Add("onclick", oci.OnClick);

            return new HtmlTag("a", oci.Id)
                        .Attrs(oci.HtmlProps)
                        .Attr("title", oci.AltText ?? "")
                        .Class(oci.DivCssClass)
                        .SetInnerText(oci.Text)
                        .ToHtml();
        }

        static OperationsContextualItem[] Empty = new OperationsContextualItem[0];

        public static List<OperationsContextualItem> GetForLite(Lite lite, object queryName, string prefix)
        {
            IdentifiableEntity ident = Database.Retrieve(lite);

            var list = OperationLogic.ServiceGetEntityOperationInfos(ident);

            var contexts =
                    from oi in list
                    let os = (EntityOperationSettings)OperationClient.Manager.Settings.TryGetC(oi.Key)
                    let ctx = new ContextualOperationContext()
                    {
                        Entity = ident,
                        QueryName = queryName,
                        OperationSettings = os,
                        OperationInfo = oi,
                        Prefix = prefix
                    }
                    let entityCtx = new EntityOperationContext() 
                    {
                        Entity = ident,
                        OperationSettings = os,
                        OperationInfo = oi,
                        Prefix = prefix
                    }
                    where string.IsNullOrEmpty(oi.CanExecute)
                        && oi.Lite == true
                        && (os == null 
                            || (os.IsContextualVisible == null && (os.IsVisible == null || (os.IsVisible != null && os.IsVisible(entityCtx)))) 
                            || (os.IsContextualVisible != null && os.IsContextualVisible(ctx)))
                    select new 
                    {
                       ContextualContext = ctx,
                       EntityContext = entityCtx
                    };               

            return contexts
                   .Select(pair => OperationButtonFactory.Create(pair.ContextualContext, pair.EntityContext))
                   .ToList();
        }
    }

    public static class JsOp
    {
        public static JsInstruction ConfirmOperation(ContextualOperationContext ctx, JsFunction onSuccess)
        {
            return Js.Confirm(
                Resources.PleaseConfirmYouDLikeToExecuteTheOperation0ToTheEntity123.Formato(ctx.OperationInfo.Key, ctx.Entity.ToStr, ctx.Entity.GetType().NiceName(), ctx.Entity.Id),
                onSuccess);
        }

        public static JsInstruction ConfirmOperation(ContextualOperationContext ctx, JsInstruction onSuccess)
        {
            return Js.Confirm(
                Resources.PleaseConfirmYouDLikeToExecuteTheOperation0ToTheEntity123.Formato(ctx.OperationInfo.Key, ctx.Entity.ToStr, ctx.Entity.GetType().NiceName(), ctx.Entity.Id), 
                onSuccess);
        }
    }
}