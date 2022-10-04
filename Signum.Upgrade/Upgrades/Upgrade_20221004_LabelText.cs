using System;
using System.IO;
using System.Net;
using System.Net.Http;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20221004_LabelText : CodeUpgradeBase
{
    public override string Description => "Replaces labelText -> label";

    public override void Execute(UpgradeContext uctx)
    {
        Regex regexFormatText = new Regex(@"\blabelText\b");

        uctx.ForeachCodeFile($@"*.tsx", file =>
        {
            file.Replace(regexFormatText, "label");
        });
    }
}
