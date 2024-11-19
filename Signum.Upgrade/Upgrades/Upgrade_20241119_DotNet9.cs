using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20241119_DotNet9 : CodeUpgradeBase
{
    public override string Description => "Update Nugets";

    public override void Execute(UpgradeContext uctx)
    {

        uctx.ForeachCodeFile("*.csproj", file =>
        {
            file.Replace(
                "<TargetFramework>net8.0</TargetFramework>",
                "<TargetFramework>net9.0</TargetFramework>");

            file.UpdateNugetReferences("""
                <PackageReference Include="Microsoft.TypeScript.MSBuild" Version="5.6.2">
                <PackageReference Include="System.DirectoryServices" Version="9.0.0" />
                <PackageReference Include="System.DirectoryServices.AccountManagement" Version="9.0.0" />
                <PackageReference Include="Microsoft.Graph" Version="5.62.0" />
                <PackageReference Include="System.Drawing.Common" Version="9.0.0" />
                <PackageReference Include="HtmlAgilityPack" Version="1.11.71" />
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
                <PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.0" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.0" />
                <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="9.0.0" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="9.0.0" />
                <PackageReference Include="System.Text.Encoding.CodePages" Version="9.0.0" />
                <PackageReference Include="Selenium.WebDriver.ChromeDriver" Version="131.0.6778.6900" />
                <PackageReference Include="Swashbuckle.AspNetCore" Version="7.0.0" />
                <PackageReference Include="Npgsql" Version="9.0.0" />
                """);
                //<PackageReference Include="Signum.TSGenerator" Version="9.0.0" />
        });
    }
}



