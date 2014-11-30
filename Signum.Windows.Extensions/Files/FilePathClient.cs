using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Windows.Processes;
using System.Reflection;
using Signum.Entities.Files;
using Signum.Utilities.Reflection;
using Signum.Utilities;

namespace Signum.Windows.Files
{
    public static class FilePathClient
    {
        public static void AsserIsStarted()
        {
            Navigator.Manager.AssertDefined(ReflectionTools.GetMethodInfo(() => FilePathClient.Start()));
        }

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<FileRepositoryEntity> { View = e => new FileRepository() },
                    new EntitySettings<FilePathEntity> { View = e => new FilePath() },
                });

                Server.SetSymbolIds<FileTypeSymbol>();
            }
        }
    }
}
