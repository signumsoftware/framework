using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20221012_UpdateNugets2 : CodeUpgradeBase
{
    public override string Description => "Updates Nuget again";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReference("Microsoft.Graph", "4.43.0");
            file.UpdateNugetReference("Microsoft.Identity.Client", "4.47.2");
            file.UpdateNugetReference("Microsoft.TypeScript.MSBuild", "4.8.4");
            file.UpdateNugetReference("Microsoft.NET.Test.Sdk", "17.3.2");
            file.UpdateNugetReference("Selenium.WebDriver", "4.5.0");
            file.UpdateNugetReference("DocumentFormat.OpenXml", "2.18.0");
        });
    }
}



