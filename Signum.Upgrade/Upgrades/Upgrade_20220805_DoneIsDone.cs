namespace Signum.Upgrade.Upgrades;

class Upgrade_20220805_DoneIsDone : CodeUpgradeBase
{
    public override string Description => "Remove .done() (rejectionhandled does it)";

    public static Regex DoneRegex = new Regex(@"\s*\.done\(\)");
    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.tsx, *.ts", file =>
        {
            file.Replace(DoneRegex, "");
        });
    }
}
