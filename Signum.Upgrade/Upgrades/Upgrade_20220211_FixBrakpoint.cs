using Signum.Utilities;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20220211_FixBrakpoint : CodeUpgradeBase
{
    public override string Description => "Change Breakpoint to sm because breakpoints round down";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile(@"Southwind.React\App\Layout.tsx", file =>
        {
            file.Replace("Breakpoints.md", "Breakpoints.sm"); 
        });
    }
}
