using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20250314_DirectoryBuildsprops : CodeUpgradeBase
{
    public override string Description => "";

    public override void Execute(UpgradeContext uctx)
    {
      
        uctx.ChangeCodeFile("Directory.Build.props", cf =>
        {
            cf.ReplaceBetweenIncluded(a => a.Contains("<PropertyGroup"), a => a.Contains("</PropertyGroup"),
                """
                <PropertyGroup>
                    <PreserveCompilationContext>false</PreserveCompilationContext>
                </PropertyGroup>
                """
                );
        });

    }

 
}



