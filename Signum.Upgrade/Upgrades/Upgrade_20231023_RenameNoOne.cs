using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20231023_RenameNoOne : CodeUpgradeBase
{
    public override string Description => "Updates rename noOne to notAny and anyNo to notAll";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.tsx, *.ts", file =>
        {
            file.Replace(new Regex(@"\.noOne\("), m => ".notAny(");
            file.Replace(new Regex(@"\.anyNo\("), m => ".notAll(");
        });
    }
}



