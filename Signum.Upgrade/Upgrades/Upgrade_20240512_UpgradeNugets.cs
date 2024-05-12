using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20240512_UpgradeNugets : CodeUpgradeBase
{
    public override string Description => "Upgrade nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile("*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Npgsql" Version="8.0.3" />
                <PackageReference Include="Microsoft.Graph" Version="5.52.0" />
                <PackageReference Include="Selenium.Support" Version="4.20.0" />
                <PackageReference Include="Selenium.WebDriver" Version="4.20.0" />
                <PackageReference Include="HtmlAgilityPack" Version="1.11.61" />
                <PackageReference Include="xunit" Version="2.8.0" /> 
                <PackageReference Include="xunit.runner.visualstudio" Version="2.8.0">
                <PackageReference Include="Net.Codecrete.QrCodeGenerator" Version="2.0.5" />
                """);
        });
    }
}



