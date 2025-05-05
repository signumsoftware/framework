using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20240816_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "Update Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile("*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="System.Drawing.Common" Version="8.0.8" />
                <PackageReference Include="HtmlAgilityPack" Version="1.11.63" />
                <PackageReference Include="Azure.Storage.Blobs" Version="12.21.2" />
                <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.11.0" />
                <PackageReference Include="Selenium.WebDriver.ChromeDriver" Version="127.0.6533.11900" />
                """);
        });
    }
}



