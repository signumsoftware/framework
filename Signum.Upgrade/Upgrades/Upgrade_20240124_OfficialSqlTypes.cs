using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20240124_OfficialSqlTypes : CodeUpgradeBase
{
    public override string Description => "Replace Unofficial.Microsoft.SqlServer.Types by the official one";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.ReplaceLine(a => a.Contains("Unofficial.Microsoft.SqlServer.Types"), """
                    <PackageReference Include="Microsoft.SqlServer.Types" Version="160.1000.6" />
                    """);
        });


    }
}



