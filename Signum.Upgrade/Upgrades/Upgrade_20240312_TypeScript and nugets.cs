using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signum.Upgrade.Upgrades;


class Upgrade_20240312_TypeScript_and_nugets : CodeUpgradeBase
{
    public override string Description => "New typescript 5.4.2 and other nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile("*.csproj", file =>
        {
            file.UpdateNugetReference("Microsoft.TypeScript.MSBuild", "5.4.2");
            file.UpdateNugetReference("Azure.Messaging.ServiceBus", "7.17.4");
            file.UpdateNugetReference("Microsoft.Data.SqlClient", "5.2.0");
            file.UpdateNugetReference("Microsoft.Graph", "5.44.0");
            file.UpdateNugetReference("Microsoft.CodeAnalysis.CSharp", "4.9.2");
            file.UpdateNugetReference("Selenium.WebDriver.ChromeDriver", "122.0.6261.11100");
            file.UpdateNugetReference("SixLabors.ImageSharp", "2.1.7");
            file.UpdateNugetReference("SkiaSharp.NativeAssets.Linux", "2.88.7");
        });

        uctx.ChangeCodeFile("Southwind/package.json", file =>
        {
            file.UpdateNpmPackage("typescript", "5.4.2");
        });
    }
}



