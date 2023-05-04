namespace Signum.Upgrade.Upgrades;

class Upgrade_20220318_UpdateNPMPagages : CodeUpgradeBase
{
    public override string Description => "Upgrade NPM Packages";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile("Southwind.React/package.json", file =>
        {
            file.UpdateNpmPackage("react-widgets", "5.8.3");
            file.UpdateNpmPackage("react-bootstrap", "2.2.1");
        });
    }
}
