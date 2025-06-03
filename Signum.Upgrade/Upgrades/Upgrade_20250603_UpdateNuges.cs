using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20250603_UpdateNuges : CodeUpgradeBase
{
    public override string Description => "Update nugets";

    public override void Execute(UpgradeContext uctx)
    {

        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="9.0.5" />
                <PackageReference Include="Microsoft.TypeScript.MSBuild" Version="5.8.3" />
                """);

        });
    }
}


