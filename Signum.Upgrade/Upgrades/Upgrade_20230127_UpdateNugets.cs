using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20230127_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "Updates Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                    <PackageReference Include="Microsoft.TypeScript.MSBuild" Version="4.9.4">
                    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.4.1" />
                    <PackageReference Include="Selenium.Support" Version="4.8.0" />
                    <PackageReference Include="Selenium.WebDriver.ChromeDriver" Version="109.0.5414.7400" />
                    <PackageReference Include="Selenium.WebDriver" Version="4.8.0" />
                    <PackageReference Include="Microsoft.Data.SqlClient" Version="5.1.0" />
                    <PackageReference Include="Npgsql" Version="7.0.1" />
                    <PackageReference Include="DocumentFormat.OpenXml" Version="2.19.0" />
                    <PackageReference Include="Microsoft.IdentityModel.Protocols.OpenIdConnect" Version="6.26.0" />
                    <PackageReference Include="Microsoft.IdentityModel.Tokens" Version="6.26.0" />
                    """);
        });
    }
}



