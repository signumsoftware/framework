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
    class Upgrade_20210108_RemoveLangVersion : CodeUpgradeBase
    {
        public override string Description => "remove <LangVersion>";


        public override void Execute(UpgradeContext uctx)
        {
            uctx.ForeachCodeFile(@"*.csproj", file =>
            {
                file.RemoveAllLines(a => a.Contains("<LangVersion>"));
            });
        }
    }
}
