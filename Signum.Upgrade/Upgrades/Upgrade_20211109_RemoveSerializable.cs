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
    class Upgrade_20211109_RemoveSerializable : CodeUpgradeBase
    {
        public override string Description => "Remove Unnecessary [Serializable] attribute from entities";

        public override void Execute(UpgradeContext uctx)
        {
            uctx.ForeachCodeFile(@"*.cs",uctx.EntitiesDirectory,  file =>
            {
                file.Replace(new Regex(@"\[\s*Serializable\s*\]"), "");
                file.Replace(new Regex(@"Serializable\s*,"), "");
            });
        }
    }
}
