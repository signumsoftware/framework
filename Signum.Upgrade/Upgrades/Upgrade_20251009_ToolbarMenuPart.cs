using Signum.Utilities;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20251009_ToolbarMenuPart : CodeUpgradeBase
{
    public override string Description => "Replaces ToolbarMenuPartEntity";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile("Southwind/Starter.cs", file =>
        {
            file.Replace("LinkListPartEntity", "ToolbarMenuPartEntity");
            file.WarningLevel = WarningLevel.None;
            file.Replace("ToolbarPartEntity", "ToolbarMenuPartEntity");
        });
    }
}
