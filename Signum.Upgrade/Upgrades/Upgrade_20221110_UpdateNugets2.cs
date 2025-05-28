using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20221110_UpdateNugets2 : CodeUpgradeBase
{
    public override string Description => "Updates Nuget 2";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                    <PackageReference Include="Signum.MSBuildTask" Version="7.0.0" />
                    <PackageReference Include="Signum.TSGenerator" Version="7.0.0" />
                    <PackageReference Include="Npgsql" Version="7.0.0" />
                    """);
        });
    }
}



