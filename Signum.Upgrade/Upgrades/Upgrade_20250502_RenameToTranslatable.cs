using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20250502_RenameToTranslatable : CodeUpgradeBase
{
    public override string Description => "rename TranslateableRouteType to TranslatableRouteType (typo fix)";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.cs", file =>
        {
            file.Replace(new Regex(@"\bTranslateableRouteType\b"), m => "TranslatableRouteType");
        });
    }
}



