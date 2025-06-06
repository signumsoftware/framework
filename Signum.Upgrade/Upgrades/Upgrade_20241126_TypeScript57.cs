using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20241126_TypeScript57 : CodeUpgradeBase
{
    public override string Description => "Upgrade to TypeScript 5.7";

    public override void Execute(UpgradeContext uctx)
    {

        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
           
            file.UpdateNugetReferences("""
                <PackageReference Include="Microsoft.TypeScript.MSBuild" Version="5.7.1">
                <PackageReference Include="DocumentFormat.OpenXml" Version="3.2.0" />
                <PackageReference Include="Selenium.Support" Version="4.27.0" />
                <PackageReference Include="Selenium.WebDriver" Version="4.27.0" />
                <PackageReference Include="Selenium.WebDriver.ChromeDriver" Version="131.0.6778.8500" />
                <PackageReference Include="Swashbuckle.AspNetCore" Version="7.1.0" />
                """);
                //<PackageReference Include="Signum.TSGenerator" Version="9.0.0" />
        });

        uctx.ChangeCodeFile("Southwind.Server/package.json", file =>
        {

            file.UpdateNpmPackages("""
                "typescript": "5.7.2",
                """);
            //<PackageReference Include="Signum.TSGenerator" Version="9.0.0" />
        });
    }
}



