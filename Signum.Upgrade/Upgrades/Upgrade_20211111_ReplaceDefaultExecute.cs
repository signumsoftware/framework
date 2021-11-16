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
    class Upgrade_20211006_ReplaceDefaultExecute : CodeUpgradeBase
    {
        public override string Description => "Replace eoc.defaultClick( with return eoc.defaultClick(";

        public override void Execute(UpgradeContext uctx)
        {
            uctx.ForeachCodeFile($@"*.tsx", uctx.ReactDirectory, file =>
            {
                file.Replace("eoc.defaultClick(", "/*TODO: fix*/ eoc.defaultClick(");
            });
        }
    }
}
