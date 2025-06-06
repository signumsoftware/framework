using System;
using System.IO;
using System.Net;
using System.Net.Http;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20220920_FormatText : CodeUpgradeBase
{
    public override string Description => "Replaces formatText -> format, and unitText -> unit";

    public override void Execute(UpgradeContext uctx)
    {
        Regex regexFormatText = new Regex(@"\bformatText\b");
        Regex regexUnitText = new Regex(@"\bunitText\b");

        uctx.ForeachCodeFile($@"*.tsx", file =>
        {
            file.Replace(regexFormatText, "format");
            file.Replace(regexUnitText, "unit");
        });
    }
}
