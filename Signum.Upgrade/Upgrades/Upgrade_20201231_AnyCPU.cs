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
    class Upgrade_20201231_AnyCPU : CodeUpgradeBase
    {
        public override string Description => "replace x64 by AnyCPU";


        public override void Execute(UpgradeContext uctx)
        {
            uctx.ForeachCodeFile(@"*.csproj", file =>
            {
                file.RemoveAllLines(a => a.Contains("<Platforms>"));
                file.ReplaceBetweenIncluded(
                    a => a.Contains("<PropertyGroup Condition="),
                    a => a.Contains("</PropertyGroup>"),
                    "");
            });

            uctx.ChangeCodeFile(@"Southwind.sln", file =>
            {
                file.Replace("x64", "Any CPU");
            });
        }
    }
}
