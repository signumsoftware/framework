namespace Signum.Upgrade.Upgrades;

class Upgrade_20261112_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "Update Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Microsoft.Extensions.Configuration" Version="11.0.0" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="11.0.0" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="11.0.0" />
                <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="11.0.0" />
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.12.0" />
                <PackageReference Include="Swashbuckle.AspNetCore" Version="11.0.1" />
                <PackageReference Include="xunit.v3" Version="4.0.3" />
                """);

            if (file.Content.Contains("\"SkiaSharp.NativeAssets.Linux\""))
                file.UpdateNugetReference("SkiaSharp.NativeAssets.Linux", "4.156.0");
        });

        // Swashbuckle 11 / Microsoft.OpenApi 2.0: Content dictionary now uses IOpenApiMediaType
        uctx.ForeachCodeFile(@"*.cs", file =>
        {
            file.Replace(
                "new Dictionary<string, OpenApiMediaType>",
                "new Dictionary<string, IOpenApiMediaType>");
        });
    }
}
