using Signum.Utilities;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20251113_Dotnet10 : CodeUpgradeBase
{
    public override string Description => "Update to .Net 10";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile("*.csproj", file =>
        {
            file.Replace(
                "<TargetFramework>net9.0</TargetFramework>",
                "<TargetFramework>net10.0</TargetFramework> ");

            file.UpdateNugetReferences("""
                <PackageReference Include="DeepL.net" Version="1.17.0" />
                <PackageReference Include="Microsoft.Data.SqlClient" Version="6.1.3" />
                <PackageReference Include="Microsoft.Extensions.Configuration" Version="10.0.0" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="10.0.0" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="10.0.0" />
                <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="10.0.0" />
                <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.0" />
                <PackageReference Include="Microsoft.Graph" Version="5.96.0" />
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.0.1" />
                <PackageReference Include="Selenium.Support" Version="4.38.0" />
                <PackageReference Include="Selenium.WebDriver" Version="4.38.0" />                
                <PackageReference Include="Signum.TSGenerator" Version="10.0.1" />
                <PackageReference Include="Signum.MSBuildTask" Version="10.0.0" />
                <PackageReference Include="Selenium.WebDriver.ChromeDriver" Version="142.0.7444.16200" />
                <PackageReference Include="Swashbuckle.AspNetCore" Version="10.0.1" />
                <PackageReference Include="System.DirectoryServices" Version="10.0.0" />
                <PackageReference Include="System.DirectoryServices" Version="10.0.0" />
                <PackageReference Include="System.DirectoryServices.AccountManagement" Version="10.0.0" />
                <PackageReference Include="System.Drawing.Common" Version="10.0.0" />
                <PackageReference Include="System.Text.Encoding.CodePages" Version="10.0.0" />
                <PackageReference Include="xunit.v3" Version="3.2.0" />
                """);
        });
    }
}
