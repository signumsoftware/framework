using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20260208_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "Update Nugets";

    public override void Execute(UpgradeContext uctx)
    {

        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Microsoft.Extensions.Configuration" Version="10.0.2" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="10.0.2" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="10.0.2" />
                <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="10.0.2" />
                <PackageReference Include="Microsoft.VisualStudio.Azure.Containers.Tools.Targets" Version="1.23.0" />
                <PackageReference Include="Swashbuckle.AspNetCore" Version="10.1.2" />
                <PackageReference Include="xunit.v3" Version="3.2.2" />
                <PackageReference Include="Selenium.WebDriver.ChromeDriver" Version="144.0.7559.13300" />
                <PackageReference Include="Selenium.WebDriver" Version="4.40.0" />
                """);

        });
    }
}