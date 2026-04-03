namespace Signum.Upgrade.Upgrades;

class Upgrade_20201124_CombinedUserChartPart : CodeUpgradeBase
{
    public override string Description => "CombinedUserChartPart";


    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile("Southwind.Logic/Starter.cs", file =>
        {
            file.Replace(
                "typeof(UserChartPartEntity),",
                "typeof(UserChartPartEntity), typeof(CombinedUserChartPartEntity),");
        });
    }
}
