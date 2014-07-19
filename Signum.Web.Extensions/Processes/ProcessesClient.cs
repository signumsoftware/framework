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
        public static JsModule Module = new JsModule("Extensions/Signum.Web.Extensions/Processes/Scripts/Processes"); 

        public static void Start(bool packages, bool packageOperations)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(ProcessesClient));

                UrlsRepository.DefaultSFUrls.Add("processFromMany", url => url.Action((ProcessController pc)=>pc.ProcessFromMany()));

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<ProcessDN>{ PartialViewName = e => ViewPrefix.Formato("Process"), },
                    new EntitySettings<ProcessAlgorithmSymbol>{ PartialViewName = e => ViewPrefix.Formato("ProcessAlgorithm") },
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

                if (MixinDeclarations.IsDeclared(typeof(ProcessDN), typeof(UserProcessSessionMixin)))
                    Navigator.EntitySettings<ProcessDN>().CreateViewOverride().AfterLine(p => p.Algorithm, 
                        (html, tc) => html.EntityLine(tc, p => p.Mixin<UserProcessSessionMixin>().User));

                SpecialOmniboxProvider.Register(new SpecialOmniboxAction("ProcessPanel", 
                    () => ProcessPermission.ViewProcessPanel.IsAuthorized(),
                    uh => uh.Action((ProcessController pc) => pc.View())));
            }
        }

        public static List<IMenuItem> CreateGroupContextualItem(SelectedItemsMenuContext ctx)
        {
            if (!Navigator.IsViewable(typeof(PackageOperationDN), null))
                return null;

            if (ctx.Lites.IsNullOrEmpty() || ctx.Lites.Count <= 1)
                return null;

            if (ctx.Implementations.IsByAll)
                return null;

            var types = ctx.Lites.Select(a => a.EntityType).Distinct().ToList();

            var contexts = (from t in types
                            from oi in OperationClient.Manager.OperationInfos(t)
                            where oi.IsEntityOperation
                            group new { t, oi } by oi.OperationSymbol into g
                            let os = OperationClient.Manager.GetSettings<EntityOperationSettings>(g.Key)
                            let oi = g.First().oi
                            let context = new ContextualOperationContext
                            {
                                OperationInfo = g.First().oi,
                                Prefix = ctx.Prefix,
                                QueryName = ctx.QueryName,
                                Entities = ctx.Lites,
                                OperationSettings = os.Try(s => s.ContextualFromMany),
                                CanExecute = OperationSymbol.NotDefinedForMessage(g.Key, types.Except(g.Select(a => a.t))),
                                Url = ctx.Url
                            }
                            where os == null ? oi.Lite == true && oi.OperationType != OperationType.ConstructorFrom :
                            os.ContextualFromMany.IsVisible == null ? (oi.Lite == true && os.IsVisible == null && oi.OperationType != OperationType.ConstructorFrom && (os.OnClick == null || os.ContextualFromMany.OnClick != null)) :
                            os.ContextualFromMany.IsVisible(context)
                            select context).ToList();

            if (contexts.IsEmpty())
                return null;

            var cleanKeys = contexts.Where(cod => cod.CanExecute == null && cod.OperationInfo.HasStates == true)
                .Select(kvp => kvp.OperationInfo.OperationSymbol).ToList();

            if (cleanKeys.Any())
            {
                Dictionary<OperationSymbol, string> canExecutes = OperationLogic.GetContextualCanExecute(ctx.Lites.ToArray(), cleanKeys);
                foreach (var cod in contexts)
                {
                    var ce = canExecutes.TryGetC(cod.OperationInfo.OperationSymbol);
                    if (ce.HasText())
                        cod.CanExecute = ce;
                }
            }

            List<IMenuItem> menuItems = contexts.Select(op => OperationClient.Manager.CreateContextual(op,
                coc => ProcessesClient.Module["processFromMany"](coc.Options(), JsFunction.Event)
                )).OrderBy(o => o.Order).Cast<IMenuItem>().ToList();


            menuItems.Insert(0, new MenuItemHeader(SearchMessage.Processes.NiceToString()));

            return menuItems;
        }
    }
}