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
    class Upgrade_20210210_UpgradeNugets : CodeUpgradeBase
    {
        public override string Description => "Upgrade a few nugets";

        public override void Execute(UpgradeContext uctx)
        {
            uctx.ChangeCodeFile(@"Southwind.React\Southwind.React.csproj", file =>
            {
                file.UpdateNugetReference("Microsoft.TypeScript.MSBuild", "4.1.4");
                file.WarningLevel = WarningLevel.Warning;
                file.UpdateNugetReference("Swashbuckle.AspNetCore", "6.0.3");
            });

        }
    }
}
