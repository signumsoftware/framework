using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20240227_UpgradeNugets : CodeUpgradeBase
{
    public override string Description => "Upgrade nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile("*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Microsoft.Graph" Version="5.43.0" />
                <PackageReference Include="Azure.Messaging.ServiceBus" Version="7.17.3" />
                <PackageReference Include="Microsoft.Data.SqlClient" Version="5.1.5" />
                <PackageReference Include="Selenium.Support" Version="4.18.1" />
                <PackageReference Include="Selenium.WebDriver" Version="4.18.1" />
                <PackageReference Include="System.Drawing.Common" Version="8.0.2" />
                <PackageReference Include="HtmlAgilityPack" Version="1.11.59" />
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.9.0" />
                <PackageReference Include="xunit" Version="2.7.0" /> 
                <PackageReference Include="xunit.runner.visualstudio" Version="2.5.7">
                <PackageReference Include="Npgsql" Version="8.0.2" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="8.0.1" />
                <PackageReference Include="Selenium.WebDriver.ChromeDriver" Version="122.0.6261.6901" />
                """);
        });
    }
}



