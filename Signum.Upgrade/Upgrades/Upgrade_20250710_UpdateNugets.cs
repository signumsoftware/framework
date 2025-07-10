using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20250710_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "Update Nugets";

    public override void Execute(UpgradeContext uctx)
    {

        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Markdig" Version="0.41.3" />
                <PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.7" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="9.0.7" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.7" />
                <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="9.0.7" />
                <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.7" />
                <PackageReference Include="Microsoft.Graph" Version="5.85.0" />
                <PackageReference Include="System.DirectoryServices" Version="9.0.7" />
                <PackageReference Include="System.DirectoryServices.AccountManagement" Version="9.0.7" />
                <PackageReference Include="System.Drawing.Common" Version="9.0.7" />
                <PackageReference Include="System.Text.Encoding.CodePages" Version="9.0.7" />
                <PackageReference Include="System.Text.Json" Version="9.0.7" />
                """);

        });
    }
}


