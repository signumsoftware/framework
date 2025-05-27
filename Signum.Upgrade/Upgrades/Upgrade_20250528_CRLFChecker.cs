using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20250528_CRLFChecker : CodeUpgradeBase
{
    public override string Description => "Add CRLFChecker in Terminal";

    public override void Execute(UpgradeContext uctx)
    {


        uctx.ChangeCodeFile("Southwind.Terminal/Program.cs", pg =>
        {
            pg.InsertBeforeFirstLine(a => a.Trim() == "try", "CRLFChecker.CheckGitCRLN();");
        });
    }
}


