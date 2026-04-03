namespace Signum.Upgrade.Upgrades;

class Upgrade_20260318_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "Update Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Microsoft.Extensions.Configuration" Version="10.0.5" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="10.0.5" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="10.0.5" />
                <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="10.0.5" />
                <PackageReference Include="Selenium.WebDriver.ChromeDriver" Version="146.0.7680.8000" />
                <PackageReference Include="Swashbuckle.AspNetCore" Version="10.1.5" />
                """);
        });
    }
}
