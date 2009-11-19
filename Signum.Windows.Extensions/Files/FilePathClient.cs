using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Windows.Processes;
using System.Reflection;
using Signum.Entities.Files;
using Signum.Utilities.Reflection;
using Signum.Utilities;

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
                Navigator.Manager.Settings.AddRange(new Dictionary<Type, EntitySettings>() 
                {
                    { typeof(FileRepositoryDN), new EntitySettings() { View = () => new FileRepository() }},
                    { typeof(FilePathDN), new EntitySettings() {View = () => new FilePath() }},
                });
            }
        }
    }
}
