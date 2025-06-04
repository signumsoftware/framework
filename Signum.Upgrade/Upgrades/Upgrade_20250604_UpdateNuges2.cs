using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20250604_UpdateNuges2 : CodeUpgradeBase
{
    public override string Description => "Update nugets";

    public override void Execute(UpgradeContext uctx)
    {

        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Microsoft.Graph" Version="5.80.0" />
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
                <PackageReference Include="Selenium.WebDriver.ChromeDriver" Version="137.0.7151.6800" />
                <PackageReference Include="Swashbuckle.AspNetCore" Version="8.1.4" />
                """);

        });
    }
}


