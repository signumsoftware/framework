namespace Signum.Upgrade.Upgrades;

class Upgrade_20220204_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "Update Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReference("Microsoft.Identity.Client", "4.40.0");
            file.UpdateNugetReference("Selenium.WebDriver.ChromeDriver", "98.0.4758.8000");
        });
    }
}
