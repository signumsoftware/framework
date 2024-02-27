using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20240208_Frozen : CodeUpgradeBase
{
    public override string Description => "Use FrozenDictionary / FrozenSet and Random.Shared";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.cs", file =>
        {
            file.Replace("ResetLazy<Dictionary<", "ResetLazy<FrozenDictionary<");
            file.Replace("ResetLazy<HashSet<", "ResetLazy<FrozenSet<");
            file.Replace("MyRandom.Current", "Random.Shared");
        });
    }
}



