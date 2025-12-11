using Signum.Utilities;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20251210_NugetUpdate : CodeUpgradeBase
{
    public override string Description => "Nuget Update";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile("*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="System.DirectoryServices" Version="10.0.1" />
                <PackageReference Include="Microsoft.Extensions.Configuration" Version="10.0.1" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="10.0.1" />
                <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="10.0.1" />
                <PackageReference Include="xunit.v3" Version="3.2.1" />
                <PackageReference Include="DeepL.net" Version="1.18.0" />
                <PackageReference Include="System.Drawing.Common" Version="10.0.1" />
                <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.1" />
                """);
        });

    }
}
