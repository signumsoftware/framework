using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Signum.Upgrade.Upgrades
{
    class Upgrade_20211027_UpdateNugets : CodeUpgradeBase
    {
        public override string Description => "Update nugets";

        public override void Execute(UpgradeContext uctx)
        {
            uctx.ForeachCodeFile(@"*.csproj", file =>
            {
                file.UpdateNugetReference("Azure.Storage.Files.Shares", "12.8.0");
                file.UpdateNugetReference("Microsoft.TypeScript.MSBuild", "4.4.4");
                file.UpdateNugetReference("Microsoft.NET.Test.Sdk", "17.0.0");
                file.UpdateNugetReference("Selenium.WebDriver.ChromeDriver", "95.0.4638.1700");
            });
        }
    }
}
