using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20240823_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "Update Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile("*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="HtmlAgilityPack" Version="1.11.64" />
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.0" />
                <PackageReference Include="Selenium.WebDriver.ChromeDriver" Version="128.0.6613.8400" />
                <PackageReference Include="Swashbuckle.AspNetCore" Version="6.7.1" />
                """);
        });
    }
}



