using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20250527_UpdateNuges : CodeUpgradeBase
{
    public override string Description => "Update nugets";

    public override void Execute(UpgradeContext uctx)
    {

        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.14.0" />
                <PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.5" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.5" />
                <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.5" />
                <PackageReference Include="Microsoft.Graph" Version="5.79.0" />
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.0" />
                <PackageReference Include="Selenium.Support" Version="4.33.0" />
                <PackageReference Include="Selenium.WebDriver" Version="4.33.0" />
                <PackageReference Include="System.DirectoryServices" Version="9.0.5" />
                <PackageReference Include="System.DirectoryServices.AccountManagement" Version="9.0.5" />
                <PackageReference Include="System.Drawing.Common" Version="9.0.5" />
                <PackageReference Include="System.Text.Encoding.CodePages" Version="9.0.5" />
                <PackageReference Include="System.Text.Json" Version="9.0.5" />
                <PackageReference Include="xunit.runner.visualstudio" Version="3.1.0">
                <PackageReference Include="xunit.v3" Version="2.0.2" />
                """);

        });
    }
}


