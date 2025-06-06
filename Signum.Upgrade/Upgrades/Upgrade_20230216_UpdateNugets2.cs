using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20230216_UpdateNugets2 : CodeUpgradeBase
{
    public override string Description => "Updates Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                    <PackageReference Include="Microsoft.Graph" Version="4.53.0" />
                    <PackageReference Include="Microsoft.Identity.Client" Version="4.50.0" />
                    <PackageReference Include="Microsoft.TypeScript.MSBuild" Version="4.9.5">
                    <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="7.0.3" />
                    <PackageReference Include="Selenium.WebDriver.ChromeDriver" Version="110.0.5481.7700" />
                    """);
        });
    }
}



