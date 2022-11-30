using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20221118_ExtraSmallToXs : CodeUpgradeBase
{
    public override string Description => "Replace ExtraSmall -> xs, etc..";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.tsx,*.ts", file =>
        {
            file.Replace("\"ExtraSmall\"", "\"xs\"");
            file.Replace("\"Small\"", "\"sm\"");
            file.Replace("\"Normal\"", "\"md\"");
            file.Replace("\"Large\"", "\"lg\"");
        });
    }
}



