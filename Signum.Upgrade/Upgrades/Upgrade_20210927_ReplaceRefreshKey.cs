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
    class Upgrade_20210927_ReplaceRefreshKey : CodeUpgradeBase
    {
        public override string Description => "Replace refreshKey?:any with deps?: React.DependencyList";


        public override void Execute(UpgradeContext uctx)
        {
            uctx.ForeachCodeFile($@"*.tsx, *.ts", uctx.ReactDirectory, file =>
            {
                file.Replace(new Regex(@"refreshKey\s*=\s*{(?<val>(?:[^{}]|(?<Open>[{])|(?<-Open>[}]))+)}"),
                    m => $@"deps={{[{m.Groups["val"].Value}]}}");
            });
        }        
    }
}
