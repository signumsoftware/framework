using Signum.Utilities;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20260109_UpateNugets : CodeUpgradeBase
{
    public override string Description => "Update Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Azure.Storage.Blobs" Version="12.27.0" />
                <PackageReference Include="DeepL.net" Version="1.19.0" />
                <PackageReference Include="DocumentFormat.OpenXml" Version="3.4.1" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="10.0.1" />
                <PackageReference Include="Microsoft.Graph" Version="5.100.0" />
                <PackageReference Include="Npgsql" Version="10.0.1" />
                <PackageReference Include="Selenium.WebDriver.ChromeDriver" Version="143.0.7499.19200" />
                <PackageReference Include="Swashbuckle.AspNetCore" Version="10.1.0" />
                <PackageReference Include="System.DirectoryServices.AccountManagement" Version="10.0.1" />
                """);
        });
    }
}
