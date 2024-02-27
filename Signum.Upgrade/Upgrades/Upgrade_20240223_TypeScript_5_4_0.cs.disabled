using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20240223_TypeScript_5_4_0 : CodeUpgradeBase
{
    public override string Description => "New typescript 5.4.0";

    public override void Execute(UpgradeContext uctx)
    {
        
        uctx.ForeachCodeFile("*.csproj", file =>
        {
            file.UpdateNugetReference("Microsoft.TypeScript.MSBuild", "5.4.0");
        });

        uctx.ChangeCodeFile("Southwind/package.json", file =>
        {
            file.UpdateNpmPackage("typescript", "5.4.0");
        });
    }
}



