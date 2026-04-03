namespace Signum.Upgrade.Upgrades;

class Upgrade_20210916_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "Update nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReference("Microsoft.Graph", "4.5.0");
            file.UpdateNugetReference("Microsoft.Graph.Auth", "1.0.0-preview.7");
            file.UpdateNugetReference("Microsoft.Identity.Client", "4.36.1");
        });
    }
}
