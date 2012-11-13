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
    public delegate ContextualItem[] GetOperationsContextualItemDelegate(Lite lite);

    public static class OperationsContextualItemsHelper
    {
        static Dictionary<Type, List<Delegate>> contextualItems = new Dictionary<Type, List<Delegate>>();

        public static void RegisterContextualItems<T>(GetOperationsContextualItemDelegate getContextualItems)
        {
            contextualItems.GetOrCreate(typeof(T)).Add(getContextualItems);
        }

        public static void Start()
        {
            ContextualItemsHelper.GetContextualItemsForLites += CreateIndividualContextualItem;
        }

        public static ContextualItem CreateIndividualContextualItem(SelectedItemsMenuContext ctx)
        {
            if (ctx.Lites.IsNullOrEmpty() || ctx.Lites.Count > 1)
                return null;

            List<ContextualItem> operations = GetForLite(ctx.Lites[0], ctx.QueryName, ctx.Prefix);
            if (operations == null || operations.Count == 0) 
                return null;

            HtmlStringBuilder content = new HtmlStringBuilder();
            using(content.Surround(new HtmlTag("ul").Class("sf-search-ctxmenu-operations")))
            {
                string ctxItemClass = "sf-search-ctxitem";

                content.AddLine(new HtmlTag("li")
                    .Class(ctxItemClass + " sf-search-ctxitem-header")
                    .InnerHtml(
                        new HtmlTag("span").InnerHtml(Resources.Search_CtxMenuItem_Operations.EncodeHtml()))
                    );

                foreach(var operation in operations)
                {
                    content.AddLine(new HtmlTag("li")
                        .Class(ctxItemClass)
                        .InnerHtml(operation.IndividualOperationToString()));
                }
            }

            return new ContextualItem
            {
                Id = TypeContextUtilities.Compose(ctx.Prefix, "ctxItemOperations"),
                Content = content.ToHtml().ToString()
            };
        }

        public static MvcHtmlString IndividualOperationToString(this ContextualItem oci)
        {
            if (oci.Enabled)
                oci.HtmlProps.Add("onclick", oci.OnClick);

            return new HtmlTag("a", oci.Id)
                        .Attrs(oci.HtmlProps)
                        .Attr("title", oci.AltText ?? "")
                        .Class("sf-operation-ctxitem")
                        .SetInnerText(oci.Text)
                        .ToHtml();
        }

        static ContextualItem[] Empty = new ContextualItem[0];

        public static List<ContextualItem> GetForLite(Lite lite, object queryName, string prefix)
        {
            IdentifiableEntity ident = Database.Retrieve(lite);

            var list = OperationLogic.ServiceGetEntityOperationInfos(ident);

            var contexts = 
                    from oi in list
                    let os = (EntityOperationSettings)OperationsClient.Manager.Settings.TryGetC(oi.Key)
                    let ctx = new ContextualOperationContext()
                    {
                        Entities = new List<Lite> { ident.ToLite() },
                        QueryName = queryName,
                        OperationSettings = os.TryCC(eos => eos.Contextual),
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
                            || (os.Contextual != null && (os.Contextual.IsVisible == null || (os.Contextual.IsVisible != null && os.Contextual.IsVisible(ctx))))
                            || (os.Contextual == null && os.OnClick == null && (os.IsVisible == null || (os.IsVisible != null && os.IsVisible(entityCtx)))))
                    select ctx;

            return contexts.Select(op => OperationButtonFactory.CreateContextual(op)).ToList();
        }
    }

    public static class JsOp
    {
        public static JsInstruction ConfirmOperation(ContextualOperationContext ctx, JsFunction onSuccess)
        {
            var msg = ctx.Entities.Count == 1 ?
                Resources.PleaseConfirmYouDLikeToExecuteTheOperation0ToTheEntity123.Formato(ctx.OperationInfo.Key, ctx.Entities[0].ToString(), ctx.Entities[0].GetType().NiceName(), ctx.Entities[0].Id) :
                Resources.PleaseConfirmYouDLikeToExecuteTheOperation0ToTheSelectedEntities.Formato(ctx.OperationInfo.Key);

            return Js.Confirm(msg, onSuccess);
        }

        public static JsInstruction ConfirmOperation(ContextualOperationContext ctx, JsInstruction onSuccess)
        {
            var msg = ctx.Entities.Count == 1 ?
                Resources.PleaseConfirmYouDLikeToExecuteTheOperation0ToTheEntity123.Formato(ctx.OperationInfo.Key, ctx.Entities[0].ToString(), ctx.Entities[0].GetType().NiceName(), ctx.Entities[0].Id) :
                Resources.PleaseConfirmYouDLikeToExecuteTheOperation0ToTheSelectedEntities.Formato(ctx.OperationInfo.Key);

            return Js.Confirm(msg, onSuccess);
        }
    }
}