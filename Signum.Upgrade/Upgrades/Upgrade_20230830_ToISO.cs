using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20230830_ToISO : CodeUpgradeBase
{
    public override string Description => "ToISO";

    public override void Execute(UpgradeContext uctx)
    {
        Regex regex = new Regex(@"\.toISO(Date|Time)?\(\)(?!!)");

        uctx.ForeachCodeFile(@"*.ts, *.tsx", file =>
        {
            file.Replace(regex, m => m.Value + "!");
        });

        
    }
}



