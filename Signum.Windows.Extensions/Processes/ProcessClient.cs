using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Processes;
using Signum.Windows.Processes;

namespace Signum.Windows.Processes
{
    public static class ProcessClient
    {
        public static void Start(bool packages)
        {
            if(Navigator.Manager.NotDefined<ProcessDN>())
            {
                Navigator.Manager.Settings.Add(typeof(ProcessDN), new EntitySettings(true) { View = () => new Process() });
                Navigator.Manager.Settings.Add(typeof(ProcessExecutionDN), new EntitySettings(false) { View = () => new ProcessExecution() });
            }

            if(packages && Navigator.Manager.NotDefined<PackageDN>())
            {
                Navigator.Manager.Settings.Add(typeof(PackageDN), new EntitySettings(true) { View = () => new Package() });
                Navigator.Manager.Settings.Add(typeof(PackageLineDN), new EntitySettings(false) { View = () => new PackageLine() }); 
            }    
        }

        internal static void AsserIsLoaded()
        {
            if (!Navigator.Manager.ContainsDefinition<ProcessDN>())
                throw new ApplicationException("Call ProcessClient.Start first"); 
        }
    }
}
