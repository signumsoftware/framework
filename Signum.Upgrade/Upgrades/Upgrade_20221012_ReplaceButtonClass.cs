using System;
using System.IO;
using System.Net;
using System.Net.Http;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20221012_ReplaceButtonClass : CodeUpgradeBase
{
    public override string Description => "Replaces badge btn-primary -> badge bg-primary...";

    public override void Execute(UpgradeContext uctx)
    {
        Regex regexButtonClass = new Regex(@"badge btn-(primary|secondary|success|warning|danger|info|light|dark)");

        uctx.ForeachCodeFile($@"*.tsx", file =>
        {
            file.Replace(regexButtonClass, m => m.ToString().Replace("btn-", "bg-"));
        });
    }
}
