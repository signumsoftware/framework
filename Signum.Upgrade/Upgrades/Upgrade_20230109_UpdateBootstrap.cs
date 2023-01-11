using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20230109_UpdateBootstrap : CodeUpgradeBase
{
    public override string Description => "Update Bootstrap";

    public override void Execute(UpgradeContext uctx)
    {

        uctx.ChangeCodeFile("Southwind.React/package.json", file =>
        {
            file.UpdateNpmPackages("""
                "bootstrap": "5.2.3",
                "react-bootstrap": "2.7.0",
                """);
        });
    }
}



