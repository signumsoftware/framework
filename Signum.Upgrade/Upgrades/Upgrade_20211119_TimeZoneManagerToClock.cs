namespace Signum.Upgrade.Upgrades;

class Upgrade_20211119_ClockToClock : CodeUpgradeBase
{
    public override string Description => "Rename TimeZoneManager -> Clock";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile("*.cs", file =>
        {
            file.Replace("TimeZoneManager", "Clock");
        });
    }
}
