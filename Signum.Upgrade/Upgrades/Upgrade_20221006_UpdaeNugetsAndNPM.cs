using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20221006_UpdaeNugetsAndNPM : CodeUpgradeBase
{
    public override string Description => "Updates Nuget and NPM packages";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReference("Azure.Storage.Files.Shares", "12.11.0");
            file.UpdateNugetReference("Microsoft.NET.Test.Sdk", "17.3.2");
            file.UpdateNugetReference("Selenium.WebDriver.ChromeDriver", "106.0.5249.6100");
            file.UpdateNugetReference("Selenium.WebDriver", "4.5.0");
        });

        uctx.ChangeCodeFile(@"Southwind.React/package.json", file =>
        {
            file.UpdateNpmPackage("@types/codemirror", "5.60.5");
            file.UpdateNpmPackage("codemirror", "5.65.9");
            file.UpdateNpmPackage("bootstrap", "5.2.2");
            file.UpdateNpmPackage("@types/luxon", "3.0.1");
            file.UpdateNpmPackage("luxon", "3.0.4");
            file.UpdateNpmPackage("@azure/msal-browser", "2.29.0");
            file.UpdateNpmPackage("@popperjs/core", "2.11.6");
            file.UpdateNpmPackage("react", "18.2.0");
            file.UpdateNpmPackage("react-bootstrap", "2.5.0");
            file.UpdateNpmPackage("react-dom", "18.2.0");
            file.UpdateNpmPackage("react-router", "5.3.4");
            file.UpdateNpmPackage("react-router-dom", "5.3.4");
            file.UpdateNpmPackage("sass", "1.55.0");
            file.UpdateNpmPackage("sass-loader", "13.0.2");
            file.UpdateNpmPackage("ts-loader", "9.4.1");
            file.UpdateNpmPackage("typescript", "4.8.4");
            file.UpdateNpmPackage("webpack", "5.74.0");
            file.UpdateNpmPackage("webpack-bundle-analyzer", "4.6.1");
            file.UpdateNpmPackage("webpack-cli", "4.10.0");
        });
    }
}



