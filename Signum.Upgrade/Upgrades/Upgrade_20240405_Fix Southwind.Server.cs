using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Signum.Upgrade.Upgrades;


class Upgrade_20240405_Fix_Southwind_Server : CodeUpgradeBase
{
    public override string Description => "fix Southwind.Server";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile("*.json", "Southwind.Terminal", c =>
        {
            if (c.FilePath.Contains("settings."))
                c.MoveFile(c.FilePath.Replace("settings.", "appsettings."));
        });

        if (Directory.Exists(uctx.AbsolutePathSouthwind("Southwind.Test.Environment"))){
            uctx.ForeachCodeFile("*.json", "Southwind.Test.Environment", c =>
            {
                if (c.FilePath.Contains("settings."))
                    c.MoveFile(c.FilePath.Replace("settings.", "appsettings."));
            });
        }

        uctx.ChangeCodeFile(@"Southwind.Test.Environment/SouthwindEnvironment.cs", c =>
        {
            c.Replace("settings.", "appsettings.");
        });

        uctx.ChangeCodeFile(@"Southwind.Terminal/Program.cs", c =>
        {
            c.Replace("settings.", "appsettings.");
        });

        uctx.ChangeCodeFile(@"Southwind.Terminal/Southwind.Terminal.csproj", c =>
        {
            c.Replace("settings.", "appsettings.");
        });

        uctx.ChangeCodeFile(@"Southwind.Test.Environment/Southwind.Test.Environment.csproj", c =>
        {
            c.Replace("settings.", "appsettings.");
        });

        uctx.ChangeCodeFile(@"Southwind/Home.tsx", c =>
        {
            c.Replace("DashboardClient => DashboardClient.API.home()", "file => file.DashboardClient.API.home()");
        });
    }
}



