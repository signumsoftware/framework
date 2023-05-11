namespace Signum.Upgrade.Upgrades;

class Upgrade_20211216_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "Update nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReference("Microsoft.TypeScript.MSBuild", "4.5.3");
            file.UpdateNugetReference("Microsoft.VisualStudio.Azure.Containers.Tools.Targets", "1.14.0");
            file.UpdateNugetReference("SciSharp.TensorFlow.Redist", "2.7.0");
            file.UpdateNugetReference("Selenium.WebDriver", "4.1.0");
        });
    }
}
