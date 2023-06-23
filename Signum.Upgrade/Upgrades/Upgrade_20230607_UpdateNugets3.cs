using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20230607_UpdateNugets3 : CodeUpgradeBase
{
    public override string Description => "Updates Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                    <PackageReference Include="Microsoft.TypeScript.MSBuild" Version="5.1.3"/>
                    <PackageReference Include="Azure.Messaging.ServiceBus" Version="7.15.0" />
                    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.6.2" />
                    <PackageReference Include="Selenium.WebDriver.ChromeDriver" Version="114.0.5735.9000" />
                    """);
        });

        uctx.ChangeCodeFile(@"Southwind/package.json", file =>
        {
            file.UpdateNpmPackages("""
                "assets-webpack-plugin": "7.1.1",
                "css-loader": "6.8.1",
                "file-loader": "6.2.0",
                "raw-loader": "4.0.2",
                "rimraf": "5.0.1",
                "sass": "1.56.1",
                "sass-loader": "13.3.1",
                "style-loader": "3.3.3",
                "ts-loader": "9.4.3",
                "typescript": "5.1.3",
                "url-loader": "4.1.1",
                "webpack": "5.86.0",
                "webpack-bundle-analyzer": "4.9.0",
                "webpack-cli": "5.1.3",
                "webpack-notifier": "1.15.0"
                """);

            file.WarningLevel = WarningLevel.None;

            file.RemoveAllLines(a => a.Contains(""""@types/react": "file:../Framework/Signum.React/node_modules/@types/react""""));
        });
    }
}



