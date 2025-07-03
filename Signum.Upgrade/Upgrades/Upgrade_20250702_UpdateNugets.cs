using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20250702_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "Update Nugets";

    public override void Execute(UpgradeContext uctx)
    {

        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Azure.Messaging.ServiceBus" Version="7.20.1" />
                <PackageReference Include="Azure.Storage.Blobs" Version="12.24.1" />
                <PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.6" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.6" />
                <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="9.0.6" />
                <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.6" />
                <PackageReference Include="Microsoft.Graph" Version="5.83.0" />
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
                <PackageReference Include="Selenium.Support" Version="4.34.0" />
                <PackageReference Include="Selenium.WebDriver" Version="4.34.0" />
                <PackageReference Include="System.DirectoryServices" Version="9.0.6" />
                <PackageReference Include="System.DirectoryServices.AccountManagement" Version="9.0.6" />
                <PackageReference Include="System.Drawing.Common" Version="9.0.6" />
                <PackageReference Include="System.Text.Encoding.CodePages" Version="9.0.6" />
                <PackageReference Include="System.Text.Json" Version="9.0.6" />
                <PackageReference Include="xunit.runner.visualstudio" Version="3.1.1">
                <PackageReference Include="xunit.v3" Version="2.0.3" />
                """);

        });
    }
}


