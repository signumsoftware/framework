namespace Signum.Upgrade.Upgrades;

class Upgrade_20260323_RemoveRegisterTranslatableRoutes : CodeUpgradeBase
{
    public override string Description => "remove obsolete RegisterTranslatableRoutes calls";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile("Southwind/Starter.cs", file =>
        {
            file.RemoveAllLines(a => a.Contains("UserQueryLogic.RegisterTranslatableRoutes();"));
            file.RemoveAllLines(a => a.Contains("UserChartLogic.RegisterTranslatableRoutes();"));
            file.RemoveAllLines(a => a.Contains("DashboardLogic.RegisterTranslatableRoutes();"));
            file.RemoveAllLines(a => a.Contains("ToolbarLogic.RegisterTranslatableRoutes();"));
        }, showWarnings: WarningLevel.None);
    }
}
