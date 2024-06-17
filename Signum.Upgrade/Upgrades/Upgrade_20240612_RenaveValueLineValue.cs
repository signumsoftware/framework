using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20240612_RenaveValueLineValue : CodeUpgradeBase
{
    public override string Description => "Rename ValueLineValue -> AutoLineValue";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.cs", "Southwind.Test.React", file =>
        {
            file.Replace("ValueLine", "AutoLine");
        });
    }
}



