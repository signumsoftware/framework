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
using Signum.Entities.Operations;
using Signum.Utilities;
using System.Windows.Controls;

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
                Navigator.AddSetting(new EntitySettings<ProcessDN>(EntityType.SystemString) { View = e => new ProcessUI(), Icon = Image("process.png") });
                Navigator.AddSetting(new EntitySettings<ProcessExecutionDN>(EntityType.System) { View = e => new ProcessExecution(), Icon = Image("processExecution.png") });

                OperationClient.AddSettings(new List<OperationSettings>()
                {
                    new EntityOperationSettings<ProcessExecutionDN>(ProcessOperation.Plan){ Icon = Image("plan.png"), Click = ProcessOperation_Plan },
                    new EntityOperationSettings<ProcessExecutionDN>(ProcessOperation.Cancel){ Icon = Image("stop.png") },
                    new EntityOperationSettings<ProcessExecutionDN>(ProcessOperation.Execute){ Icon = Image("play.png") },
                    new EntityOperationSettings<ProcessExecutionDN>(ProcessOperation.Suspend){ Icon = Image("pause.png") },
                });

                if (packageOperation || package)
                    Navigator.AddSetting(new EntitySettings<PackageLineDN>(EntityType.System) { View = e => new PackageLine(), Icon = Image("packageLine.png") });

                if (package)
                    Navigator.AddSetting(new EntitySettings<PackageDN>(EntityType.System) { View = e => new Package(), Icon = Image("package.png") });

                if (packageOperation)
                {
                    Navigator.AddSetting(new EntitySettings<PackageOperationDN>(EntityType.System) { View = e => new PackageOperation(), Icon = Image("package.png") });

                    SearchControl.GetContextMenuItems += SearchControl_GetContextMenuItems;
                }
            }
        }

         class PackageData
        {
            public Enum OperationKey; 

            public Dictionary<Type, OperationInfo> OperationInfos;

            public EntityOperationSettingsBase Settings;

            public string CanExecute;
        }



        static IEnumerable<MenuItem> SearchControl_GetContextMenuItems(SearchControl sc)
        {
            if (sc.SelectedItems.IsNullOrEmpty() || sc.SelectedItems.Length == 1)
                return null;

            if (sc.Implementations.IsByAll)
                return null;

            var result = (from t in sc.Implementations.Types
                          from oi in OperationClient.Manager.OperationInfos(t)
                          where oi.IsEntityOperation && oi.Lite == true
                          group KVP.Create(t, oi) by oi.Key into g
                          let os = (EntityOperationSettingsBase)OperationClient.Manager.Settings.TryGetC(g.Key)
                          where os == null ? true:
                                os.ContextualFromMany == null ? !os.ClickOverriden :
                                os.ContextualFromMany.OnVisible(sc, g.First().Value)
                          select new PackageData
                          {
                              OperationKey = g.Key,
                              OperationInfos = g.ToDictionary(),
                              CanExecute = null,
                              Settings = os
                          }).ToList();

            if (result.IsEmpty())
                return null;

            var types = sc.SelectedItems.Select(a=>a.RuntimeType).Distinct().ToList();

            foreach (PackageData pomi in result)
            {
                if (!types.All(pomi.OperationInfos.ContainsKey))
                    pomi.CanExecute = "{0} is not defined for {1}".Formato(types.Where(t => !pomi.OperationInfos.ContainsKey(t)).CommaAnd(a => a.NiceName()));
            }

            var cleanKeys = result.Where(pomi => pomi.CanExecute == null && pomi.OperationInfos.Values.Any(oi => oi.HasStates))
                .Select(kvp => kvp.OperationKey).ToList();

            if (cleanKeys.Any())
            {
                Dictionary<Enum, string> canExecutes = Server.Return((IOperationServer os) => os.GetContextualCanExecute(sc.SelectedItems, cleanKeys));
                foreach (var pomi in result)
                {
                    var ce = canExecutes.TryGetC(pomi.OperationKey);
                    if (ce.HasText())
                        pomi.CanExecute = ce;
                }
            }

            return result.Select(pd=>PackageOperationMenuItemConsturctor.Construct(sc, pd.OperationKey, pd.CanExecute, pd.OperationInfos, pd.Settings));
        }


        static ProcessExecutionDN ProcessOperation_Plan(EntityOperationEventArgs<ProcessExecutionDN> args)
        {
            DateTime plan = TimeZoneManager.Now;
            if (ValueLineBox.Show(ref plan, "Choose planned date", "Please, choose the date you want the process to start", "Planned date", null, null, Window.GetWindow(args.SenderButton)))
            {
                return  ((ProcessExecutionDN)args.Entity).ToLite().ExecuteLite(ProcessOperation.Plan, plan); 
            }
            return null; 
        }

        static BitmapFrame Image(string name)
        {
            return ImageLoader.LoadIcon(PackUriHelper.Reference("Images/" + name, typeof(ProcessClient)));
        }
    }
}
