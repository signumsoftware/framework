using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20231108_TruncDateTime : CodeUpgradeBase
{
    public override string Description => "Update Node Script in Dockerfile";

    public override void Execute(UpgradeContext uctx)
    {
        var hourStart = new Regex(@"\bHourStart\b");
        var minuteStart = new Regex(@"\bMinuteStart\b");
        var secondStart = new Regex(@"\bMinuteStart\b");
        var trimToHours = new Regex(@"\bTrimToHours\b");
        var trimToMinutes = new Regex(@"\bTrimToMinutes\b");
        var trimToSeconds = new Regex(@"\bTrimToSeconds\b");

        uctx.ForeachCodeFile(@"*.cs", file =>
        {
            file.Replace(hourStart, "TruncHours");
            file.Replace(minuteStart, "TruncMinutes");
            file.Replace(secondStart, "TruncSeconds");
            file.Replace(trimToHours, "TruncHours");
            file.Replace(trimToMinutes, "TruncMinutes");
            file.Replace(trimToSeconds, "TruncSeconds");
        });
    }
}



