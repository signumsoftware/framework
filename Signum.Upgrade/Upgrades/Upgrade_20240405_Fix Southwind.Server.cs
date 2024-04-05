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
        uctx.ForeachCodeFile("settings.*.json", "Southwind.Terminal", c =>
        {
            c.MoveFile(c.FilePath.Replace("settings.", "appsettings."));
        });

        uctx.ForeachCodeFile("settings.*.json", "Southwind.Test.Environment", c =>
        {
            c.MoveFile(c.FilePath.Replace("settings.", "appsettings."));
        });

        uctx.ChangeCodeFile(@"Southwind.Terminal/Program.cs", c =>
        {
            c.Replace("settings.", "appsettings.");
        });

        uctx.ChangeCodeFile(@"Southwind.Test.Environment/SpitzleiEnvironment.cs", c =>
        {
            c.Replace("settings.", "appsettings.");
        });
    }
}



