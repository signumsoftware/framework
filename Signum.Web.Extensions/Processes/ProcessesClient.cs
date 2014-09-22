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
using Signum.Utilities.Reflection;

namespace Signum.Web.Processes
{
    public static class ProcessClient
    {
        public static string ViewPrefix = "~/processes/Views/{0}.cshtml";
        public static JsModule Module = new JsModule("Extensions/Signum.Web.Extensions/Processes/Scripts/Processes"); 

        public static void Start(bool packages, bool packageOperations)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(ProcessClient), "Processes");

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
                    Navigator.EntitySettings<ProcessDN>().CreateViewOverrides().AfterLine(p => p.Algorithm, 
                        (html, tc) => html.EntityLine(tc, p => p.Mixin<UserProcessSessionMixin>().User));

                SpecialOmniboxProvider.Register(new SpecialOmniboxAction("ProcessPanel", 
                    () => ProcessPermission.ViewProcessPanel.IsAuthorized(),
                    uh => uh.Action((ProcessController pc) => pc.View())));
            }
        }

        static readonly GenericInvoker<Func<SelectedItemsMenuContext, OperationInfo, ContextualOperationSettingsBase, IContextualOperationContext>> newContextualOperationContext =
         new GenericInvoker<Func<SelectedItemsMenuContext, OperationInfo, ContextualOperationSettingsBase, IContextualOperationContext>>((ctx, oi, settings) =>
             new ContextualOperationContext<IdentifiableEntity>(ctx, oi, (ContextualOperationSettings<IdentifiableEntity>)settings));


        public static List<IMenuItem> CreateGroupContextualItem(SelectedItemsMenuContext ctx)
        {
            if (!Navigator.IsViewable(typeof(PackageOperationDN), null))
                return null;

            if (ctx.Lites.IsNullOrEmpty() || ctx.Lites.Count <= 1)
                return null;

            if (ctx.Implementations.IsByAll)
                return null;

            var type = ctx.Lites.Select(a => a.EntityType).Distinct().Only();

            if (type == null)
                return null;

            var contexts = (from oi in OperationClient.Manager.OperationInfos(type)
                            where oi.IsEntityOperation
                            let os = OperationClient.Manager.GetSettings<EntityOperationSettingsBase>(type, oi.OperationSymbol)
                            let coc = newContextualOperationContext.GetInvoker(os.Try(a => a.OverridenType) ?? type)(ctx, oi, os.Try(a => a.ContextualFromManyUntyped))
                            where os == null ? oi.Lite == true && oi.OperationType != OperationType.ConstructorFrom :
                                !os.ContextualFromManyUntyped.HasIsVisible ? (oi.Lite == true && !os.HasIsVisible && oi.OperationType != OperationType.ConstructorFrom && (!os.HasClick || os.ContextualFromManyUntyped.HasClick)) :
                                os.ContextualFromManyUntyped.OnIsVisible(coc)
                            select coc).ToList();

            if (contexts.IsEmpty())
                return null;

            var cleanKeys = contexts.Where(cod => cod.CanExecute == null && cod.OperationInfo.HasStates == true)
                .Select(kvp => kvp.OperationInfo.OperationSymbol).ToList();

            if (cleanKeys.Any())
            {
                Dictionary<OperationSymbol, string> canExecutes = OperationLogic.GetContextualCanExecute(ctx.Lites, cleanKeys);
                foreach (var cod in contexts)
                {
                    var ce = canExecutes.TryGetC(cod.OperationInfo.OperationSymbol);
                    if (ce.HasText())
                        cod.CanExecute = ce;
                }
            }

            List<IMenuItem> menuItems = contexts.Select(op => OperationClient.Manager.CreateContextual(op,
                coc => ProcessClient.Module["processFromMany"](coc.Options(), JsFunction.Event)
                )).OrderBy(o => o.Order).Cast<IMenuItem>().ToList();


            menuItems.Insert(0, new MenuItemHeader(SearchMessage.Processes.NiceToString()));

            return menuItems;
        }
    }
}