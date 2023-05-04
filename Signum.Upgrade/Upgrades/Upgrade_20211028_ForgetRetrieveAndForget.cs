namespace Signum.Upgrade.Upgrades;

class Upgrade_20211028_ForgetRetrieveAndForget : CodeUpgradeBase
{
    public override string Description => "Renames RetrieveAndForget -> Retrieve, fetchAndForget -> fetch";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.cs", file =>
        {
            file.Replace("RetrieveAndForget", "Retrieve");
        });

        uctx.ForeachCodeFile(@"*.ts, *.tsx", file =>
        {
            file.Replace("fetchAndForget", "fetch");
        });
    }
}
