using System;
using System.IO;
using System.Net;
using System.Net.Http;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20220912_BindParent : CodeUpgradeBase
{
    public override string Description => "Replaces NotifyChildProperty/NotifyCollectionChanged/RebindEvents to BindParent";

    public override void Execute(UpgradeContext uctx)
    {
        Regex regex = new Regex(@"\b(NotifyChildProperty|NotifyCollectionChanged|RebindEvents)\b");

        uctx.ForeachCodeFile($@"*.cs", file =>
        {
            file.Replace(regex, "BindParent");
            file.Replace("BindParent, BindParent", "BindParent");
        });
    }
}
