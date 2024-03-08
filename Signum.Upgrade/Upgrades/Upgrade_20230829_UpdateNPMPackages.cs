using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20230829_UpdateNPMPackages : CodeUpgradeBase
{
    public override string Description => "Updates NPM Packages";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile(@"Southwind/package.json", file =>
          {
              file.UpdateNpmPackages("""
                    "sass": "1.61.0",
                    "sass-loader": "13.3.2",
                    "ts-loader": "9.4.4",
                    "typescript": "5.2.2",
                    "webpack": "5.88.2",
                    "webpack-cli": "5.1.4",
                    """);
          });
    }
}



