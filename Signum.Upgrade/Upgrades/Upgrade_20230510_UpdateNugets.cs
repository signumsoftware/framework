using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20230510_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "Updates Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                    <PackageReference Include="DeepL.net" Version="1.7.1" />
                    <PackageReference Include="Microsoft.Graph" Version="5.9.0" />
                    <PackageReference Include="Azure.Storage.Blobs" Version="12.16.0" />
                    <PackageReference Include="Selenium.Support" Version="4.9.1" />
                    <PackageReference Include="Selenium.WebDriver" Version="4.9.1" />
                    <PackageReference Include="Selenium.WebDriver.ChromeDriver" Version="113.0.5672.6300" />
                    <PackageReference Include="DeepL.net" Version="1.7.1" />
                    <PackageReference Include="LibGit2Sharp" Version="0.27.2" />
                    <PackageReference Include="Npgsql" Version="7.0.4" />
                    """);
        });
    }
}



