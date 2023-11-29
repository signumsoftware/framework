using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20231030_AbstractIcons : CodeUpgradeBase
{
    public override string Description => "Abstract Icons";

    public override void Execute(UpgradeContext uctx)
    {
        var regex = new Regex(@"\[[""'](?<prefix>far|fas)[""'] *, *(?<icon>[""'][\w\d\-]*[""'])\]");

        uctx.ForeachCodeFile(@"*.tsx", file =>
        {
            file.Replace(regex,e => e.Groups["icon"].Value);
        });
    }
}



