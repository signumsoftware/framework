using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Windows.Processes;
using System.Reflection;
using Signum.Entities.Files;
using Signum.Utilities.Reflection;

namespace Signum.Windows.Extensions.Files
{
    public static class FilePathClient
    {
        internal static void AsserIsStarted()
        {
            Navigator.Manager.AssertDefined(ReflectionTools.GetMethodInfo(() => FilePathClient.Start()));
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
