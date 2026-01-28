using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20251002_UpdateNuges : CodeUpgradeBase
{
    public override string Description => "Update nugets";

    public override void Execute(UpgradeContext uctx)
    {

        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Azure.Storage.Blobs" Version="12.25.1" />
                <PackageReference Include="HtmlAgilityPack" Version="1.12.3" />
                <PackageReference Include="Microsoft.Data.SqlClient" Version="6.1.1" />
                <PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.9" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="9.0.9" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.9" />
                <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="9.0.9" />
                <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.9" />
                <PackageReference Include="Microsoft.Graph" Version="5.93.0" />
                <PackageReference Include="Selenium.Support" Version="4.35.0" />
                <PackageReference Include="Selenium.WebDriver" Version="4.35.0" />
                <PackageReference Include="Selenium.WebDriver.ChromeDriver" Version="141.0.7390.5400" />
                <PackageReference Include="Signum.TSGenerator" Version="9.2.1" />
                <PackageReference Include="Swashbuckle.AspNetCore" Version="9.0.5" />
                <PackageReference Include="System.DirectoryServices" Version="9.0.9" />
                <PackageReference Include="System.DirectoryServices.AccountManagement" Version="9.0.9" />
                <PackageReference Include="System.Drawing.Common" Version="9.0.9" />
                <PackageReference Include="System.Text.Encoding.CodePages" Version="9.0.9" />
                <PackageReference Include="System.Text.Json" Version="9.0.9" />
                <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5">
                <PackageReference Include="xunit.v3" Version="3.1.0" />
                """);

        });
    }
}


