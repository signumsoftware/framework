using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20230519_UpdateNugets2 : CodeUpgradeBase
{
    public override string Description => "Updates Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                    <PackageReference Include="Microsoft.Graph" Version="5.11.0" />
                    <PackageReference Include="Azure.Messaging.ServiceBus" Version="7.14.0" />
                    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.6.0" />
                    """);
        });
    }
}



