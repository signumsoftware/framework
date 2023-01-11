using Signum.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20221213_Diff : CodeUpgradeBase
{
    public override string Description => "Add diff reference to package.json";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile("Southwind.React/package.json", file =>
        {
            file.InsertBeforeFirstLine(a => a.Contains("\"d3\""), """
                "@types/diff" : "5.0.2",
                "diff": "5.1.0",
                """);
        });
    }
}



