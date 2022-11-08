using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20221108_DotNet7 : CodeUpgradeBase
{
    public override string Description => "Updates to .Net 7";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.Replace("<TargetFramework>net6.0</TargetFramework>", "<TargetFramework>net7.0</TargetFramework>");
        });
    }
}



