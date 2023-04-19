using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20230419_Checkbox : CodeUpgradeBase
{
    public override string Description => "Fix Checkbox";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.tsx, *.ts", file =>
        {
            file.Replace("Checkbox_StartChecked", "Checkbox_Checked");
            file.Replace("Checkbox_StartUnchecked", "Checkbox_Unchecked");
            file.Replace("NotCheckbox_StartChecked", "NotCheckbox_Checked");
            file.Replace("NotCheckbox_StartUnchecked", "NotCheckbox_Unchecked");
        });

    }
}



