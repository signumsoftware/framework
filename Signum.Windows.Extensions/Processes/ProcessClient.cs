using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Processes;
using Signum.Windows.Processes;
using Signum.Windows.Operations;
using System.Windows.Media.Imaging;
using Signum.Entities;
using Signum.Services;
using System.Reflection;
using Signum.Utilities.Reflection;
using System.Windows;
using Signum.Utilities;
using System.Windows.Controls;
using Signum.Entities.Basics;

namespace Signum.Windows.Processes
{
    public static class ProcessClient
    {
        public static void AsserIsStarted()
        {
            Navigator.Manager.AssertDefined(ReflectionTools.GetMethodInfo(() => Start(true, true)));
        }

        public static void Start(bool package, bool packageOperation)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSetting(new EntitySettings<ProcessAlgorithmSymbol> { View = e => new ProcessAlgorithm(), Icon = Image("processAlgorithm.png") });
                Navigator.AddSetting(new EntitySettings<ProcessEntity> { View = e => new ProcessUI(), Icon = Image("process.png") });


                Server.SetSymbolIds<ProcessAlgorithmSymbol>();

                OperationClient.AddSettings(new List<OperationSettings>()
                {
                    new EntityOperationSettings<ProcessEntity>(ProcessOperation.Plan){ Icon = Image("plan.png"), Click = ProcessOperation_Plan },
                    new EntityOperationSettings<ProcessEntity>(ProcessOperation.Cancel){ Icon = Image("stop.png") },
                    new EntityOperationSettings<ProcessEntity>(ProcessOperation.Execute){ Icon = Image("play.png") },
                    new EntityOperationSettings<ProcessEntity>(ProcessOperation.Suspend){ Icon = Image("pause.png") },
                });

                if (packageOperation || package)
                    Navigator.AddSetting(new EntitySettings<PackageLineEntity> { View = e => new PackageLine(), Icon = Image("packageLine.png") });

                if (package)
                    Navigator.AddSetting(new EntitySettings<PackageEntity> { View = e => new Package(), Icon = Image("package.png") });

                if (packageOperation)
                {
                    Navigator.AddSetting(new EntitySettings<PackageOperationEntity> { View = e => new PackageOperation(), Icon = Image("package.png") });

                    SearchControl.GetContextMenuItems += SearchControl_GetContextMenuItems;
                }
            }
        }

        static readonly GenericInvoker<Func<SearchControl, OperationInfo, ContextualOperationSettingsBase, IContextualOperationContext>> newContextualOperationContext =
            new GenericInvoker<Func<SearchControl, OperationInfo, ContextualOperationSettingsBase, IContextualOperationContext>>((sc, oi, settings) =>
                new ContextualOperationContext<Entity>(sc, oi, (ContextualOperationSettings<Entity>)settings));

        static IEnumerable<MenuItem> SearchControl_GetContextMenuItems(SearchControl sc)
        {
            if (!Navigator.IsViewable(typeof(PackageOperationEntity)))
                return Enumerable.Empty<MenuItem>();

            if (sc.SelectedItems.IsNullOrEmpty() || sc.SelectedItems.Count == 1)
                return null;

            if (sc.Implementations.IsByAll)
                return null;

            var type = sc.SelectedItems.Select(a => a.EntityType).Distinct().Only();

            if (type == null)
                return null;

            var result = (from oi in OperationClient.Manager.OperationInfos(type)
                          where oi.IsEntityOperation
                          let os = OperationClient.Manager.GetSettings<EntityOperationSettingsBase>(type, oi.OperationSymbol)
                          let coc = newContextualOperationContext.GetInvoker(os.Try(a => a.OverridenType) ?? type)(sc, oi, os.Try(a => a.ContextualFromManyUntyped))
                          where os == null ? oi.Lite == true && oi.OperationType != OperationType.ConstructorFrom :
                              !os.ContextualFromManyUntyped.HasIsVisible ? (oi.Lite == true && !os.HasIsVisible && oi.OperationType != OperationType.ConstructorFrom && (!os.HasClick || os.ContextualFromManyUntyped.HasClick)) :
                              os.ContextualFromManyUntyped.OnIsVisible(coc)
                          select coc).ToList();

            if (result.IsEmpty())
                return null;

            var cleanKeys = result
                .Where(cod => cod.CanExecute == null && cod.OperationInfo.HasStates == true)
                .Select(cod => cod.OperationInfo.OperationSymbol).ToList();

            if (cleanKeys.Any())
            {
                Dictionary<OperationSymbol, string> canExecutes = Server.Return((IOperationServer os) => os.GetContextualCanExecute(sc.SelectedItems, cleanKeys));
                foreach (var cod in result)
                {
                    var ce = canExecutes.TryGetC(cod.OperationInfo.OperationSymbol);
                    if (ce.HasText())
                        cod.CanExecute = ce;
                }
            }

            return result.Select(coc=>PackageOperationMenuItemConsturctor.Construct(coc));
        }


        static ProcessEntity ProcessOperation_Plan(EntityOperationContext<ProcessEntity> args)
        {
            DateTime plan = TimeZoneManager.Now;
            if (ValueLineBox.Show(ref plan, "Choose planned date", "Please, choose the date you want the process to start", "Planned date", null, null, Window.GetWindow(args.SenderButton)))
            {
                return  ((ProcessEntity)args.Entity).ToLite().ExecuteLite(ProcessOperation.Plan, plan); 
            }
            return null; 
        }

        static BitmapSource Image(string name)
        {
            return ImageLoader.LoadIcon(PackUriHelper.Reference("Images/" + name, typeof(ProcessClient)));
        }
    }
}
