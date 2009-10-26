using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Windows.Processes;
using System.Reflection;
using Signum.Entities.Files;

namespace Signum.Windows.Extensions.Files
{
    public static class FilePathClient
    {
        internal static void AsserIsStarted()
        {
            Navigator.Manager.AssertDefined(typeof(ProcessClient).GetMethod("Start"));
        }

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.Manager.Settings.Add(typeof(FileRepositoryDN), new EntitySettings() { View = () => new FileRepository() });
            }
        }
    }
}
