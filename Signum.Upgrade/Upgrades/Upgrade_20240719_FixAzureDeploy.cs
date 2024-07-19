using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20240719_FixAzureDeploy : CodeUpgradeBase
{
    public override string Description => "Add commit checks in the deploy*.ps1 scripts";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"deploy*.ps1", uctx.RootFolder, file =>
        {
            file.InsertBeforeFirstLine(a => a.StartsWith("az account"),
               """
               $status = git status --porcelain
               if ($status) { throw "There are uncommitted changes." }

               """);

            file.Replace("git push origin master:", "git push origin HEAD:");
        });
    }
}



