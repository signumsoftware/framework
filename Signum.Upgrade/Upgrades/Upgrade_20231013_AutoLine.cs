using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20231013_AutoLine : CodeUpgradeBase
{
    public override string Description => "Updates ValueLine => AutoLine";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.tsx, *.ts", file =>
        {
            file.Replace(new Regex(@"ValueLine"), m => "AutoLine");
        });
    }
}



