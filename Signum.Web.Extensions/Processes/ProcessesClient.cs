using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Authorization;
using Signum.Engine.Authorization;
using System.Reflection;
using Signum.Web.Operations;
using Signum.Entities;
using System.Web.Mvc;
using System.Diagnostics;
using Signum.Engine;
using Signum.Entities.Basics;
using Signum.Entities.Reflection;
using System.Linq.Expressions;
using Signum.Engine.Maps;
using System.Web.Routing;
using Signum.Entities.Processes;
using Signum.Engine.Operations;
using Signum.Web.Omnibox;
using Signum.Web.PortableAreas;

namespace Signum.Web.Processes
{
    public static class ProcessesClient
    {
        public static string ViewPrefix = "~/processes/Views/{0}.cshtml";

        public static void Start(bool packages, bool packageOperations)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(ProcessesClient));

                UrlsRepository.DefaultSFUrls.Add("processFromMany", url => url.Action("ProcessFromMany", "Process"));

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<ProcessDN>{ PartialViewName = e => ViewPrefix.Formato("Process"), },
                    new EntitySettings<ProcessAlgorithmDN>{ PartialViewName = e => ViewPrefix.Formato("ProcessAlgorithm") },
                    new EntitySettings<UserProcessSessionDN>{ PartialViewName = e => ViewPrefix.Formato("UserProcessSession") }
                });

                if (packages || packageOperations)
                {
                    Navigator.AddSetting(new EntitySettings<PackageLineDN> { PartialViewName = e => ViewPrefix.Formato("PackageLine") });
                }

                if (packages)
                {
                    Navigator.AddSetting(new EntitySettings<PackageDN> { PartialViewName = e => ViewPrefix.Formato("Package") });
                }

                if (packageOperations)
                {
                    Navigator.AddSetting(new EntitySettings<PackageOperationDN> { PartialViewName = e => ViewPrefix.Formato("PackageOperation") });

                    ContextualItemsHelper.GetContextualItemsForLites += CreateGroupContextualItem;
                }

                SpecialOmniboxProvider.Register(new SpecialOmniboxAction("ProcessPanel", 
                    () => ProcessPermission.ViewProcessPanel.IsAuthorized(),
                    uh => uh.Action((ProcessController pc) => pc.View())));
            }
        }

        public static ContextualItem CreateGroupContextualItem(SelectedItemsMenuContext ctx)
        {
            if (!Navigator.IsViewable(typeof(PackageOperationDN), null))
                return null;

            if (ctx.Lites.IsNullOrEmpty() || ctx.Lites.Count <= 1)
                return null;

            if (ctx.Implementations.IsByAll)
                return null;

            var types = ctx.Lites.Select(a => a.EntityType).Distinct().ToList();

            var contexts = (from t in types
                            from oi in OperationsClient.Manager.OperationInfos(t)
                            where oi.IsEntityOperation
                            group new { t, oi } by oi.Key into g
                            let os = OperationsClient.Manager.GetSettings<EntityOperationSettings>(g.Key)
                            let oi = g.First().oi
                            let context = new ContextualOperationContext
                            {
                                OperationInfo = g.First().oi,
                                Prefix = ctx.Prefix,
                                QueryName = ctx.QueryName,
                                Entities = ctx.Lites,
                                OperationSettings = os.TryCC(s => s.ContextualFromMany),
                                CanExecute = OperationDN.NotDefinedFor(g.Key, types.Except(g.Select(a => a.t))),
                            }
                            where os == null ? oi.Lite == true && oi.OperationType != OperationType.ConstructorFrom :
                            os.ContextualFromMany.IsVisible == null ? (oi.Lite == true && os.IsVisible == null && oi.OperationType != OperationType.ConstructorFrom && (os.OnClick == null || os.ContextualFromMany.OnClick != null)) :
                            os.ContextualFromMany.IsVisible(context)
                            select context).ToList();

            if (contexts.IsEmpty())
                return null;

            var cleanKeys = contexts.Where(cod => cod.CanExecute == null && cod.OperationInfo.HasStates == true)
                .Select(kvp => kvp.OperationInfo.Key).ToList();

            if (cleanKeys.Any())
            {
                Dictionary<Enum, string> canExecutes = OperationLogic.GetContextualCanExecute(ctx.Lites.ToArray(), cleanKeys);
                foreach (var cod in contexts)
                {
                    var ce = canExecutes.TryGetC(cod.OperationInfo.Key);
                    if (ce.HasText())
                        cod.CanExecute = ce;
                }
            }

            List<ContextualItem> operations = contexts.Select(op => OperationsClient.Manager.CreateContextual(op)).OrderBy(o => o.Order).ToList();

            HtmlStringBuilder content = new HtmlStringBuilder();
            using (content.Surround(new HtmlTag("ul").Class("sf-search-ctxmenu-operations")))
            {
                string ctxItemClass = "sf-search-ctxitem";

                content.AddLine(new HtmlTag("li")
                    .Class(ctxItemClass + " sf-search-ctxitem-header")
                    .InnerHtml(new HtmlTag("span").InnerHtml(SearchMessage.Search_CtxMenuItem_Operations.NiceToString().EncodeHtml())));

                foreach (var operation in operations)
                {
                    content.AddLine(new HtmlTag("li")
                        .Class(ctxItemClass)
                        .InnerHtml(OperationsClient.Manager.IndividualOperationToString(operation)));
                }
            }

            return new ContextualItem
            {
                Id = TypeContextUtilities.Compose(ctx.Prefix, "ctxItemOperations"),
                Content = content.ToHtml().ToString()
            };
        }

    }
}