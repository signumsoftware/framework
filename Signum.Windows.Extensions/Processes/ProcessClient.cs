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

namespace Signum.Windows.Processes
{
    public static class ProcessClient
    {
        public static void Start(bool packages)
        {
            if(Navigator.Manager.NotDefined<ProcessDN>())
            {
                Navigator.Manager.Settings.Add(typeof(ProcessDN), new EntitySettings(EntityType.ServerOnly) { View = () => new Process(), IsReadOnly = a => true, IsCreable = a => false, Icon = Image("process.png")  });
                Navigator.Manager.Settings.Add(typeof(ProcessExecutionDN), new EntitySettings(EntityType.ServerOnly) { View = () => new ProcessExecution(),Icon = Image("processExecution.png") });

                OperationClient.Manager.Settings.Add(ProcessOperation.FromProcess, new EntityOperationSettings { Icon = Image("execute.png") });
                OperationClient.Manager.Settings.Add(ProcessOperation.Plan, new EntityOperationSettings { Icon = Image("plan.png"), Click = ProcessOperation_Plan });
                OperationClient.Manager.Settings.Add(ProcessOperation.Cancel, new EntityOperationSettings { Icon = Image("stop.png") });
                OperationClient.Manager.Settings.Add(ProcessOperation.Execute, new EntityOperationSettings { Icon = Image("play.png") });
                OperationClient.Manager.Settings.Add(ProcessOperation.Suspend, new EntityOperationSettings { Icon = Image("pause.png") });
            }

            if(packages && Navigator.Manager.NotDefined<PackageDN>())
            {
                Navigator.Manager.Settings.Add(typeof(PackageDN), new EntitySettings(EntityType.ServerOnly) { View = () => new Package(), Icon = Image("package.png") });
                Navigator.Manager.Settings.Add(typeof(PackageLineDN), new EntitySettings(EntityType.ServerOnly) { View = () => new PackageLine(), IsReadOnly = a => true, IsCreable = a => false, Icon = Image("packageLine.png") }); 
            }    
        }

        static IdentifiableEntity ProcessOperation_Plan(EntityOperationEventArgs args)
        {
            DateTime plan = DateTime.Now;
            if (ValueLineBox.Show(ref plan, "Choose planned date", "Please, choose the date you want the process to start", "Planned date", null, null, args.SenderButton.FindCurrentWindow()))
            {
                return Server.Service<IOperationServer>().ExecuteOperationLazy(args.Entity.ToLazy(), args.OperationInfo.Key, plan); 
            }
            return null; 
        }

        internal static void AsserIsLoaded()
        {
            if (!Navigator.Manager.ContainsDefinition<ProcessDN>())
                throw new ApplicationException("Call ProcessClient.Start first"); 
        }

        static BitmapFrame Image(string name)
        {
            return ImageLoader.LoadIcon(PackUriHelper.Reference("Images/" + name, typeof(ProcessClient)));
        }
    }
}
