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
    class Upgrade_20211028_ForgetRetrieveAndForget : CodeUpgradeBase
    {
        public override string Description => "Renames RetrieveAndForget -> Retrieve, fetchAndForget -> fetch";

        public override void Execute(UpgradeContext uctx)
        {
            uctx.ForeachCodeFile(@"*.cs", file =>
            {
                file.Replace("RetrieveAndForget", "Retrieve");
            });

            uctx.ForeachCodeFile(@"*.ts, *.tsx", file =>
            {
                file.Replace("fetchAndForget", "fetch");
            });
        }
    }
}
