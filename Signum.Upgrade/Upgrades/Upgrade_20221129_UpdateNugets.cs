using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20221129_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "Updates Nuget 2";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                    <PackageReference Include="DeepL" Version="0.4.3" />
                    <PackageReference Include="Microsoft.Graph" Version="4.48.0" />
                    <PackageReference Include="Microsoft.Identity.Client" Version="4.48.1" />
                    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.4.0" />
                    <PackageReference Include="Microsoft.TypeScript.MSBuild" Version="4.9.3">
                    <PackageReference Include="Azure.Storage.Files.Shares" Version="12.12.1" />
                    <PackageReference Include="System.ServiceModel.Duplex" Version="4.10.0" />
                    <PackageReference Include="System.ServiceModel.Federation" Version="4.10.0" />
                    <PackageReference Include="System.ServiceModel.Http" Version="4.10.0" />
                    <PackageReference Include="System.ServiceModel.NetTcp" Version="4.10.0" />
                    <PackageReference Include="System.ServiceModel.Security" Version="4.10.0" />
                    """);
        });

        uctx.ChangeCodeFile("Southwind.React/package.json", file =>
        {
            file.UpdateNpmPackages("""
                "typescript": "4.9.3",
                "css-loader": "6.7.2",
                "sass": "1.56.1",
                "sass-loader": "13.2.0",
                "webpack": "5.75.0",
                "webpack-bundle-analyzer": "4.7.0",
                "webpack-cli": "5.0.0",
                """);
        });
    }
}



