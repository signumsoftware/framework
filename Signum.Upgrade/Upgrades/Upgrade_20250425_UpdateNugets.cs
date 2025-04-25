using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20250425_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "Upgrade Nugets";

    public override void Execute(UpgradeContext uctx)
    {

        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.4" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="9.0.4" />
                <PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.4" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.4" />
                <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="9.0.4" />
                <PackageReference Include="xunit.v3" Version="2.0.1" />
                <PackageReference Include="Microsoft.TypeScript.MSBuild" Version="5.8.3">
                <PackageReference Include="HtmlAgilityPack" Version="1.12.1" />
                """);
        });

    }

 
}



