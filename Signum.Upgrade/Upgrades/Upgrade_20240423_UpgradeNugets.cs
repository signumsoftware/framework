using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20240424_UpgradeNugets : CodeUpgradeBase
{
    public override string Description => "Upgrade nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile("*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Selenium.WebDriver" Version="4.19.0" />
                <PackageReference Include="Microsoft.TypeScript.MSBuild" Version="5.4.4"/>
                <PackageReference Include="SixLabors.ImageSharp" Version="2.1.8" />
                """);
        });
    }
}



