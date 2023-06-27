using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20230607_UpdateNugets4 : CodeUpgradeBase
{
    public override string Description => "Updates Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                    <PackageReference Include="Microsoft.Graph" Version="5.15.0" />
                    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.6.3" />
                    <PackageReference Include="Selenium.WebDriver" Version="4.10.0" />
                    <PackageReference Include="Microsoft.Identity.Web" Version="2.12.4" />
                    <PackageReference Include="Microsoft.Identity.Web.MicrosoftGraph" Version="2.12.4" />
                    """);
        });

        
    }
}



