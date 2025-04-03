using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20250403_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "";

    public override void Execute(UpgradeContext uctx)
    {

        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Microsoft.Graph" Version="5.75.0" />
                <PackageReference Include="Selenium.Support" Version="4.30.0" />
                <PackageReference Include="Selenium.WebDriver" Version="4.30.0" />
                """);
        });

    }

 
}



