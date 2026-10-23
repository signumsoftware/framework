namespace Signum.Upgrade.Upgrades;

class Upgrade_20261023_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "Update Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Swashbuckle.AspNetCore" Version="10.3.1" />
                <PackageReference Include="Microsoft.Graph" Version="6.9.0" />
                <PackageReference Include="Microsoft.Playwright" Version="1.64.1" />
                <PackageReference Include="Markdig" Version="1.4.2" />
                <PackageReference Include="OpenTelemetry" Version="1.20.1" />
                <PackageReference Include="OpenTelemetry.Exporter.Console" Version="1.20.1" />
                <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.20.1" />
                <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.20.1" />
                <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.20.1" />
                <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.20.1" />
                """);
        });
    }
}
