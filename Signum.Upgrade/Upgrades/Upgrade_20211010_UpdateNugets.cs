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
    class Upgrade_20211010_UpdateNugets : CodeUpgradeBase
    {
        public override string Description => "Update nugets";

        public override void Execute(UpgradeContext uctx)
        {
            uctx.ForeachCodeFile(@"*.csproj", file =>
            {
                file.UpdateNugetReference("Swashbuckle.AspNetCore", "6.2.2");
            });
        }
    }
}
