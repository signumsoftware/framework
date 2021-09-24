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
    class Upgrade_20210924_RemoveParentTokenParentValue : CodeUpgradeBase
    {
        public override string Description => "Remove parentToken / parentValue from FindOptions";

        public override void Execute(UpgradeContext uctx)
        {
            Regex regex = new Regex(@"parentToken\s*:\s*(?<parentToken>[^,]+),(\s|\n)+parentValue\s*:\s*(?<parentValue>[^,}]+)");

            uctx.ForeachCodeFile(@"*.csproj", new[] {
                uctx.RootFolder,
                Path.Combine(uctx.RootFolder, "Framework/Signum.React"),
                Path.Combine(uctx.RootFolder, "Framework/Signum.React.Extensions")
            }, file =>
            {
                file.Replace(regex, r => $"filterOptions: [{{ token: {r.Groups["parentToken"]}, value: {r.Groups["parentValue"]}}}]");
            });
        }
    }
}
