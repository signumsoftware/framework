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
    class Upgrade_20211009_MicrosoftDataSqlClient : CodeUpgradeBase
    {
        public override string Description => "Blue Green Deployments";

        public override void Execute(UpgradeContext uctx)
        {
            uctx.ForeachCodeFile(@"*.csproj", file =>
            {
                file.ReplaceLine(a => a.Contains("System.Data.SqlClient"), @"<PackageReference Include=""Microsoft.Data.SqlClient"" Version=""3.0.0"" />");
                file.ReplaceLine(a => a.Contains("dotMorten.Microsoft.SqlServer.Types"), @"<PackageReference Include=""Unofficial.Microsoft.SqlServer.Types"" Version=""2.0.1"" />");
            });

            uctx.ForeachCodeFile(@"*.cs", file =>
            {
                file.ReplaceLine(a => a.Contains("using System.Data.SqlClient;"), @"using Microsoft.Data.SqlClient.Server;
using Microsoft.Data.SqlClient;");
            });
        }
    }
}
