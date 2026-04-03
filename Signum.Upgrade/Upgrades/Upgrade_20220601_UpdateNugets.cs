namespace Signum.Upgrade.Upgrades;

class Upgrade_20220601_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "Update Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReference("Microsoft.VisualStudio.Azure.Containers.Tools.Targets", "1.15.1");
            file.UpdateNugetReference("Swashbuckle.AspNetCore", "6.3.1");
            file.UpdateNugetReference("Selenium.WebDriver.ChromeDriver", "102.0.5005.6102");
            file.UpdateNugetReference("Selenium.WebDriver", "4.2.0");
            file.UpdateNugetReference("Microsoft.TypeScript.MSBuild", "4.7.2");
        });

        uctx.ChangeCodeFile($@"Southwind.React/package.json", file =>
        {
            file.UpdateNpmPackage("ts-loader", "9.3.0");
            file.UpdateNpmPackage("typescript", "4.7.2");
            file.UpdateNpmPackage("webpack", "5.72.1");
        });
    }
}
