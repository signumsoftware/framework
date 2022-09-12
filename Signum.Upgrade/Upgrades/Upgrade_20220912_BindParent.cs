using System;
using System.IO;
using System.Net;
using System.Net.Http;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20220912_BindParent : CodeUpgradeBase
{
    public override string Description => "Replaces NotifyChildProperty/NotifyCollectionChanged/RebingEvents to BindParent";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile($@"*.cs", uctx.EntitiesDirectory, file =>
        {
            file.Replace("NotifyChildProperty", "BindParent");
            file.Replace("NotifyCollectionChanged", "BindParent");
            file.Replace("RebindEvents", "BindParent");
            file.Replace("BindParent, BindParent", "BindParent");
        });
    }
}
