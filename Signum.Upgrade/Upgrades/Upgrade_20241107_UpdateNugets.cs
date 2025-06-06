using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20241107_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "Update Nugets";

    public override void Execute(UpgradeContext uctx)
    {

        uctx.ForeachCodeFile("*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="DocumentFormat.OpenXml" Version="3.1.1" />
                <PackageReference Include="Microsoft.TypeScript.MSBuild" Version="5.6.2">
                <PackageReference Include="Signum.TSGenerator" Version="8.6.0" />
                <PackageReference Include="System.DirectoryServices.AccountManagement" Version="8.0.1" />
                <PackageReference Include="Microsoft.Graph" Version="5.61.0" />
                <PackageReference Include="Azure.Messaging.ServiceBus" Version="7.18.2" />
                <PackageReference Include="Microsoft.Data.SqlClient" Version="5.2.2" />
                <PackageReference Include="Azure.Storage.Blobs" Version="12.22.2" />
                <PackageReference Include="Selenium.Support" Version="4.26.1" />
                <PackageReference Include="Selenium.WebDriver" Version="4.26.1" />
                <PackageReference Include="DeepL.net" Version="1.10.0" />
                <PackageReference Include="System.Drawing.Common" Version="8.0.10" />
                <PackageReference Include="HtmlAgilityPack" Version="1.11.70" />
                <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="8.0.1" />
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
                <PackageReference Include="Npgsql" Version="8.0.5" />
                <PackageReference Include="xunit" Version="2.9.2" />
                """);
        });
    }
}



