namespace Signum.Upgrade.Upgrades;

class Upgrade_20220601_Dockerfile : CodeUpgradeBase
{
    public override string Description => "Update Dockerfile to Node 16";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile(@"Southwind.React\Dockerfile", file =>
        {
            file.Replace("https://deb.nodesource.com/setup_15.x", "https://deb.nodesource.com/setup_16.x");
        });
    }
}
