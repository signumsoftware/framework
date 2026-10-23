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
                <PackageReference Include="Markdig" Version="1.4.2" />
                <PackageReference Include="Microsoft.Graph" Version="6.9.0" />
                <PackageReference Include="Microsoft.Playwright" Version="1.64.1" />
                """);

            if (file.Content.Contains("\"SkiaSharp.NativeAssets.Linux\""))
                file.UpdateNugetReference("SkiaSharp.NativeAssets.Linux", "4.153.1");

            if (file.Content.Contains("\"SixLabors.ImageSharp.Drawing\""))
                file.RemoveNugetReference("SixLabors.ImageSharp.Drawing");
        });
    }
}
