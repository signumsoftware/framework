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
    class Upgrade_20211108_Net6 : CodeUpgradeBase
    {
        public override string Description => "Upgrade to .Net 6";

        public override void Execute(UpgradeContext uctx)
        {
            uctx.ForeachCodeFile("*.csproj", file =>
            {
                file.Replace(@"<TargetFramework>net5.0</TargetFramework>", @"<TargetFramework>net6.0</TargetFramework>");

                file.UpdateNugetReference("Microsoft.Extensions.Configuration", "6.0.0");
                file.UpdateNugetReference("Microsoft.Extensions.Configuration.Binder", "6.0.0");
                file.UpdateNugetReference("Microsoft.Extensions.Configuration.Json", "6.0.0");
                file.UpdateNugetReference("Microsoft.Extensions.Configuration.UserSecrets", "6.0.0");
                file.UpdateNugetReference("Signum.TSGenerator", "6.0.0");
                file.UpdateNugetReference("Selenium.WebDriver.ChromeDriver", "95.0.4638.6900");
            });

            uctx.ChangeCodeFile("Southwind.Terminal/Program.cs", file =>
            {
                file.Replace("AddUserSecrets<Program>()", "AddUserSecrets<Program>(optional: true)\n");
            });


            uctx.ForeachCodeFile("*.cs", file =>
            {
                file.Replace(new Regex("\bDate\b"), "DateOnly");
            });
        }
    }
}
