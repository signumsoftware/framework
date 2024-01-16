using Signum.Utilities;
using System;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20240116_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "Updates Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                    <PackageReference Include="DocumentFormat.OpenXml" Version="3.0.1" />
                    <PackageReference Include="Microsoft.Graph" Version="5.38.0" />
                    <PackageReference Include="Microsoft.Data.SqlClient" Version="5.1.4" />
                    <PackageReference Include="DocumentFormat.OpenXml" Version="3.0.1" />
                    <PackageReference Include="System.Drawing.Common" Version="8.0.1" />
                    <PackageReference Include="HtmlAgilityPack" Version="1.11.57" />
                    <PackageReference Include="xunit" Version="2.6.6" /> 
                    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.6">
                    """);
        });


    }
}



