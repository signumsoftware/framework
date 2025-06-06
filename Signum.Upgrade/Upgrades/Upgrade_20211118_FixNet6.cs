namespace Signum.Upgrade.Upgrades;

class Upgrade_20211118_FixNet6 : CodeUpgradeBase
{
    public override string Description => "Update Docker and MSBuildTask to .Net 6 and update to Typescript 4.5";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile("*.cs", file =>
        {
            file.Replace("TimeSpanPrecisionValidator", "TimePrecisionValidator");
        });

        uctx.ForeachCodeFile("*.csproj", file =>
        {
            file.UpdateNugetReference("Signum.MSBuildTask", "6.0.0");
            file.UpdateNugetReference("Selenium.WebDriver.ChromeDriver", "96.0.4664.4500");
            file.UpdateNugetReference("Microsoft.TypeScript.MSBuild", "4.5.2");
        });

        uctx.ChangeCodeFile(@"Southwind.React\Dockerfile", file =>
        {
            file.Replace("aspnet:5.0.9-buster-slim", "aspnet:6.0.0-bullseye-slim");
            file.Replace("sdk:5.0.400-buster-slim", "sdk:6.0.100-bullseye-slim");
        });

        uctx.ChangeCodeFile(@"Southwind.React\package.json", file =>
        {
            file.UpdateNpmPackage("typescript", "4.5.2");
        });

        uctx.ChangeCodeFile(".editorconfig", file =>
        {
            file.InsertAfterFirstLine(line => line.Contains("indent_size = 4"), "csharp_style_namespace_declarations = file_scoped:warning");
        });
    }
}
