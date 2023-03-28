using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20230328_TypeScript5AndMicrosoftGraph5 : CodeUpgradeBase
{
    public override string Description => "TypeScript 5.0.2 and Microsoft.Graph 5.3";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                    <PackageReference Include="Microsoft.TypeScript.MSBuild" Version="5.0.2">
                    <PackageReference Include="Microsoft.Graph" Version="5.3.0" />
                    <PackageReference Include="Selenium.WebDriver" Version="4.8.2" />
                    """);


            file.UpdateNpmPackages("""
                    "typescript": "5.0.2",
                    "@types/diff" : "5.0.3",
                    """);
        });
    }
}



