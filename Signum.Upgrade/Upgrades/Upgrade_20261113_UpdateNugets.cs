namespace Signum.Upgrade.Upgrades;

class Upgrade_20261113_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "Update Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Microsoft.Data.SqlClient" Version="7.1.2" />
                <PackageReference Include="Microsoft.Data.SqlClient.Extensions.Azure" Version="7.1.2" />
                <PackageReference Include="Microsoft.Extensions.Configuration" Version="11.0.0" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="11.0.0" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="11.0.0" />
                <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="11.0.0" />
                <PackageReference Include="Microsoft.Extensions.Logging" Version="11.0.0" />
                <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="11.0.0" />
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.12.0" />
                <PackageReference Include="Swashbuckle.AspNetCore" Version="11.0.1" />
                <PackageReference Include="xunit.v3" Version="4.0.3" />
                <PackageReference Include="System.DirectoryServices" Version="11.0.0" />
                <PackageReference Include="System.DirectoryServices.AccountManagement" Version="11.0.0" />
                <PackageReference Include="System.Drawing.Common" Version="11.0.0" />
                <PackageReference Include="System.Security.Cryptography.Pkcs" Version="11.0.0" />
                <PackageReference Include="OpenTelemetry" Version="1.21.0" />
                <PackageReference Include="OpenTelemetry.Exporter.Console" Version="1.21.0" />
                <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.21.0" />
                <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.21.0" />
                <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.21.0" />
                <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.21.0" />
                <PackageReference Include="Azure.Messaging.ServiceBus" Version="7.21.0" />
                <PackageReference Include="DeepL.net" Version="1.23.0" />
                <PackageReference Include="DocumentFormat.OpenXml" Version="3.5.1" />
                <PackageReference Include="HtmlAgilityPack" Version="1.13.1" />
                <PackageReference Include="Markdig" Version="1.6.0" />
                <PackageReference Include="Microsoft.Graph" Version="6.10.0" />
                <PackageReference Include="Npgsql" Version="10.0.3" />
                <PackageReference Include="Selenium.Support" Version="4.52.0" />
                <PackageReference Include="Selenium.WebDriver" Version="4.52.0" />
                """);

            // Previous upgrades only ever matched "SkiaSharp.NativeAssets.Linux", so the variants actually
            // referenced since Upgrade_20261014_ImageSharpToSkiaSharp were never updated.
            file.UpdateNugetReferences("""
                <PackageReference Include="SkiaSharp" Version="4.156.0" />
                <PackageReference Include="SkiaSharp.NativeAssets.Linux" Version="4.156.0" />
                <PackageReference Include="SkiaSharp.NativeAssets.Linux.NoDependencies" Version="4.156.0" />
                """);
        });
    }
}
