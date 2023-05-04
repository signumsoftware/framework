namespace Signum.Upgrade.Upgrades;

class Upgrade_20211010_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "Update nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReference("Swashbuckle.AspNetCore", "6.2.2");
        });
    }
}
