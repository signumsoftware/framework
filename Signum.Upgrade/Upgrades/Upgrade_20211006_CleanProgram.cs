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
    class Upgrade_20211006_CleanProgram : CodeUpgradeBase
    {
        public override string Description => "Clean Program.cs in Terminal";

        public override void Execute(UpgradeContext uctx)
        {
            uctx.ChangeCodeFile(@"Southwind.Terminal\Program.cs", file =>
            {
                file.ReplaceLine(a => a.Contains(@"{""N"", NewDatabase},"), @"{""N"", Administrator.NewDatabase},");
                file.ReplaceLine(a => a.Contains(@"{""S"", Synchronize},"), @"{""S"", Administrator.Synchronize},");
                file.ReplaceBetween(a => a.Contains("public static void NewDatabase()"), +0, a => a.Contains(@"Console.WriteLine(""Done."");"), +1, "");
                file.ReplaceBetween(a => a.Contains("static void Synchronize()"), +0, a => a.Contains(@"command.OpenSqlFileRetry();"), +1, "");
            });
        }
    }
}
