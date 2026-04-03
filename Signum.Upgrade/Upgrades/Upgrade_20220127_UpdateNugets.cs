namespace Signum.Upgrade.Upgrades;

class Upgrade_20220127_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "Update Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReference("DocumentFormat.OpenXml", "2.15.0");
            file.UpdateNugetReference("Selenium.WebDriver.ChromeDriver", "97.0.4692.7100");
        });
    }
}
