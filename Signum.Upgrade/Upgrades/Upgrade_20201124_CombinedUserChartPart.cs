using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Upgrade.Upgrades
{
    class Upgrade_20201124_CombinedUserChartPart : CodeUpgradeBase
    {
        public override string Description => "CombinedUserChartPart";

        public override string SouthwindCommitHash => "5d87563412c238710d184528853d65f67848d03c";

        public override void Execute(UpgradeContext uctx)
        {
            uctx.ChangeCodeFile("Southwind.Logic/Starter.cs", file =>
            {
                file.Replace(
                    "typeof(UserChartPartEntity),",
                    "typeof(UserChartPartEntity), typeof(CombinedUserChartPartEntity),");
            });
        }
    }
}
