#region usings
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
using Signum.Web.Properties;
using System.Diagnostics;
using Signum.Engine;
using Signum.Entities.Basics;
using Signum.Entities.Reflection;
using Signum.Entities.Operations;
using System.Linq.Expressions;
using Signum.Engine.Maps;
using System.Web.Routing;
using Signum.Entities.Processes;
using Signum.Engine.Operations;
#endregion

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

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<ProcessExecutionDN>(EntityType.System){ PartialViewName = e => ViewPrefix.Formato("ProcessExecution"), },
                    new EntitySettings<ProcessDN>(EntityType.SystemString){ PartialViewName = e => ViewPrefix.Formato("Process")},
                });

                if (packages || packageOperations)
                {
                    Navigator.AddSetting(new EntitySettings<PackageLineDN>(EntityType.System) { PartialViewName = e => ViewPrefix.Formato("PackageLine") });
                }

                if (packages)
                {
                    Navigator.AddSetting(new EntitySettings<PackageDN>(EntityType.System) { PartialViewName = e => ViewPrefix.Formato("Package") });
                }

                if (packageOperations)
                {
                    Navigator.AddSetting(new EntitySettings<PackageOperationDN>(EntityType.System) { PartialViewName = e => ViewPrefix.Formato("PackageOperation") });
                    OperationsClient.AddSetting(new ContextualOperationSettings(PackageOperationOperation.CreatePackageOperation)
                    {
                        IsVisible = _ => false,
                    });

                    ContextualItemsHelper.GetContextualItemsForLites += CreateGroupContextualItem;
                }
            }
        }

        public static ContextualItem CreateGroupContextualItem(SelectedItemsMenuContext ctx)
        {
            if (ctx.Lites.IsNullOrEmpty() || ctx.Lites.Count <= 1)
                return null;

            if (ctx.Implementations.IsByAll)
                return null;

            List<ContextualItem> operations = GetOperations(ctx);
            if (operations == null || operations.Count == 0)
                return null;

            HtmlStringBuilder content = new HtmlStringBuilder();
            using (content.Surround(new HtmlTag("ul").Class("sf-search-ctxmenu-operations")))
            {
                string ctxItemClass = "sf-search-ctxitem";

                content.AddLine(new HtmlTag("li")
                    .Class(ctxItemClass + " sf-search-ctxitem-header")
                    .InnerHtml(new HtmlTag("span").InnerHtml(Signum.Web.Extensions.Properties.Resources.Search_CtxMenuItem_Operations.EncodeHtml())));

                foreach (var operation in operations)
                {
                    content.AddLine(new HtmlTag("li")
                        .Class(ctxItemClass)
                        .InnerHtml(OperationsContextualItemsHelper.IndividualOperationToString(operation)));
                }
            }

            return new ContextualItem
            {
                Id = TypeContextUtilities.Compose(ctx.Prefix, "ctxItemOperations"),
                Content = content.ToHtml().ToString()
            };
        }

        private static List<ContextualItem> GetOperations(SelectedItemsMenuContext ctx)
        {
            var contexts = (from t in ctx.Implementations.Types
                            from oi in OperationLogic.ServiceGetOperationInfos(t)
                            where oi.IsEntityOperation && oi.Lite == true
                            let os = (EntityOperationSettings)OperationsClient.Manager.Settings.TryGetC(oi.Key)
                            group Tuple.Create(t, oi, os) by oi.Key into g
                            let context = new ContextualOperationContext
                            {
                                OperationInfo = g.First().Item2,
                                Prefix = ctx.Prefix,
                                QueryName = ctx.QueryName,
                                Entities = ctx.Lites,
                                OperationSettings = g.First().Item3.TryCC(eos => eos.ContextualFromMany)
                            }
                            let entityCtx = new EntityOperationContext()
                            {
                                OperationSettings = g.First().Item3,
                                OperationInfo = g.First().Item2,
                                Prefix = ctx.Prefix
                            }
                            where string.IsNullOrEmpty(context.OperationInfo.CanExecute)
                                && ((entityCtx.OperationSettings == null && context.OperationSettings == null)
                                    || (context.OperationSettings != null && (context.OperationSettings.IsVisible == null || (context.OperationSettings.IsVisible != null && context.OperationSettings.IsVisible(context))))
                                    || (context.OperationSettings == null && entityCtx.OperationSettings.OnClick == null && entityCtx.OperationSettings.IsVisible == null))
                            select context
                    );

            return contexts.Select(op => OperationButtonFactory.CreateContextual(op)).ToList();
        }
    }
}