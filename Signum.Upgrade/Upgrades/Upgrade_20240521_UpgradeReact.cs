using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20240521_UpgradeReact : CodeUpgradeBase
{
    public override string Description => "Upgrade @types/react";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile("package.json", file =>
        {
            file.UpdateNpmPackage("@types/react", "18.3.2");
        });
    }
}



