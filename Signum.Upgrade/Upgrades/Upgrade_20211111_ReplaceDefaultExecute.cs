namespace Signum.Upgrade.Upgrades;

class Upgrade_20211111_ReplaceDefaultExecute : CodeUpgradeBase
{
    public override string Description => "Replace eoc.defaultClick( with return eoc.defaultClick(";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile($@"*.tsx", uctx.ReactDirectory, file =>
        {
            file.Replace("eoc.defaultClick(", "/*TODO: fix*/ eoc.defaultClick(");
        });
    }
}
