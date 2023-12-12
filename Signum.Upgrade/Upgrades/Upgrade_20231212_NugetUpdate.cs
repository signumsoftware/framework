using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20231212_NugetUpdate : CodeUpgradeBase
{
    public override string Description => "Updates Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                    <PackageReference Include="System.DirectoryServices" Version="8.0.0" />
                    <PackageReference Include="System.DirectoryServices.AccountManagement" Version="8.0.0" />
                    <PackageReference Include="Microsoft.Graph" Version="5.36.0" />
                    <PackageReference Include="Microsoft.TypeScript.MSBuild" Version="5.3.3">
                    <PackageReference Include="DocumentFormat.OpenXml" Version="3.0.0" />
                    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.8.0" />
                    <PackageReference Include="DeepL.net" Version="1.8.0" />
                    <PackageReference Include="System.Drawing.Common" Version="8.0.0" />
                    <PackageReference Include="HtmlAgilityPack" Version="1.11.54" />
                    <PackageReference Include="Microsoft.Data.SqlClient" Version="5.1.2" />
                    <PackageReference Include="Npgsql" Version="8.0.1" />
                    """);
        });
    }
}



