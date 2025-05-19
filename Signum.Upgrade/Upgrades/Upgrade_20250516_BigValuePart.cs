using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20250516_BigValuePart : CodeUpgradeBase
{
    public override string Description => "Replace UserQueryPart to BigValuePart in UserAssets.xml";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"Southwind.Terminal/*.xml", file =>
        {
            file.ProcessLines(lines =>
            {
                var changed = false;
                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];

                    if (line.Contains("<UserQueryPart") && line.Contains("BigValue"))
                    {
                        lines[i] = line.Replace("<UserQueryPart", "<BigValuePart");
                        changed = true;
                    }
                }
                return changed;
            });
        });
    }
}



