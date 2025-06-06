using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20240524_UpgradeNugets : CodeUpgradeBase
{
    public override string Description => "Upgrade nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile("*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Microsoft.TypeScript.MSBuild" Version="5.4.4"/>
                <PackageReference Include="Microsoft.Graph" Version="5.54.0" />
                <PackageReference Include="Azure.Storage.Blobs" Version="12.20.0" />
                <PackageReference Include="Selenium.Support" Version="4.21.0" />
                <PackageReference Include="Selenium.WebDriver" Version="4.21.0" />
                <PackageReference Include="System.Drawing.Common" Version="8.0.5" />
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.10.0" />
                <PackageReference Include="Selenium.WebDriver.ChromeDriver" Version="125.0.6422.7600" />
                <PackageReference Include="Selenium.WebDriver" Version="4.21.0" />
                <PackageReference Include="Azure.Identity" Version="1.11.3" />
                """);
        });
    }
}



