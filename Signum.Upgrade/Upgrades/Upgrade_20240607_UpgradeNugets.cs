using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20240607_UpgradeNugets : CodeUpgradeBase
{
    public override string Description => "Upgrade nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile("*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Microsoft.Graph" Version="5.56.0" />
                <PackageReference Include="Microsoft.Data.SqlClient" Version="5.2.1" />
                <PackageReference Include="System.Drawing.Common" Version="8.0.6" />
                <PackageReference Include="xunit" Version="2.8.1" /> 
                <PackageReference Include="xunit.runner.visualstudio" Version="2.8.1">
                <PackageReference Include="Microsoft.Data.SqlClient" Version="5.2.1" />
                <PackageReference Include="Selenium.WebDriver.ChromeDriver" Version="125.0.6422.14100" />
                """);
        });
    }
}



