using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Upgrade.Upgrades
{
    class Upgrade_20210302_TypeScript42: CodeUpgradeBase
    {
        public override string Description => "Upgrade to Typescript 4.2";

        public override void Execute(UpgradeContext uctx)
        {
            uctx.ForeachCodeFile("*.csproj", file =>
            {
                file.UpdateNugetReference(@"Microsoft.TypeScript.MSBuild", @"4.2.2");
                file.UpdateNugetReference(@"Microsoft.NET.Test.Sdk", @"16.9.1");
                file.UpdateNugetReference(@"Selenium.Chrome.WebDriver", @"85.0.0");
            });

            uctx.ChangeCodeFile("Southwind.React/package.json", file =>
            {
                file.UpdateNpmPackage("ts-loader", "8.0.17");
                file.UpdateNpmPackage("typescript", "4.2.2");
            });
        }
    }
}
