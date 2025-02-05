using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20250202_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Azure.Messaging.ServiceBus" Version="7.18.3" />
                <PackageReference Include="DeepL.net" Version="1.13.0" />
                <PackageReference Include="HtmlAgilityPack" Version="1.11.72" />
                <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.12.0" />
                <PackageReference Include="Microsoft.Data.SqlClient" Version="6.0.1" />
                <PackageReference Include="Microsoft.Data.SqlClient" Version="6.0.1" />
                <PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.1" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="9.0.1" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.1" />
                <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="9.0.1" />
                <PackageReference Include="Microsoft.Graph" Version="5.69.0" />
                <PackageReference Include="Npgsql" Version="9.0.2" />
                <PackageReference Include="Selenium.Support" Version="4.28.0" />
                <PackageReference Include="Selenium.WebDriver" Version="4.28.0" />
                <PackageReference Include="Selenium.WebDriver.ChromeDriver" Version="132.0.6834.15900" />
                <PackageReference Include="System.DirectoryServices" Version="9.0.1" />
                <PackageReference Include="System.DirectoryServices.AccountManagement" Version="9.0.1" />
                <PackageReference Include="System.Drawing.Common" Version="9.0.1" />
                <PackageReference Include="System.Text.Encoding.CodePages" Version="9.0.1" />
                <PackageReference Include="xunit" Version="2.9.3" />
                <PackageReference Include="xunit.runner.visualstudio" Version="3.0.1">
                """);
        });
    }
}



