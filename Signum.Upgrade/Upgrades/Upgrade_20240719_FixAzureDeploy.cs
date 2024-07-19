using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20240719_FixAzureDeploy : CodeUpgradeBase
{
    public override string Description => "";

    public override void Execute(UpgradeContext uctx)
    {
    

        uctx.ForeachCodeFile(@"deploy*.ps1", "/", file =>
        {
            file.InsertBeforeFirstLine(a => a.StartsWith("az accoount"),
               """
               $status = git status --porcelain
               if ($status) { throw "There are uncommitted changes." }
               """);
        });
    }
}



