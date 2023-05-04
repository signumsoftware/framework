namespace Signum.Upgrade.Upgrades;

class Upgrade_20210415_ReactWidgets503 : CodeUpgradeBase
{
    public override string Description => "Upgrade React Widgets from 5.0.0 to 5.0.3 to remove warnings";

    public override void Execute(UpgradeContext uctx)
    {

        uctx.ChangeCodeFile(@"Southwind.React/package.json", file =>
        {
         
            file.UpdateNpmPackage("react-widgets", "5.0.3");
        });
    }
}
