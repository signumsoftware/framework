namespace Signum.Upgrade.Upgrades;

class Upgrade_20261016_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "Update Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Microsoft.Extensions.Configuration" Version="10.0.13" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="10.0.13" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="10.0.13" />
                <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="10.0.13" />
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.11.0" />
                <PackageReference Include="xunit.v3" Version="4.0.2" />
                <PackageReference Include="xunit.runner.visualstudio" Version="4.0.1" />
                <PackageReference Include="Swashbuckle.AspNetCore" Version="10.3.0" />
                <PackageReference Include="Azure.Storage.Blobs" Version="12.30.1" />
                <PackageReference Include="Markdig" Version="1.4.1" />
                """);
        });
    }
}
