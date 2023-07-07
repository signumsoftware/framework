using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20230707_UpdateNugets5 : CodeUpgradeBase
{
    public override string Description => "Updates Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                    <PackageReference Include="SciSharp.TensorFlow.Redist" Version="2.11.4" />
                    <PackageReference Include="xunit" Version="2.5.0" />
                    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.0">
                    <PackageReference Include="Microsoft.TypeScript.MSBuild" Version="5.1.5">
                    <PackageReference Include="SciSharp.TensorFlow.Redist" Version="2.11.4" />
                    """);
        });

        
    }
}



