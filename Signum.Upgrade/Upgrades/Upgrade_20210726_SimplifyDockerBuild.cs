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
    class Upgrade_20210726_SimplifyDockerBuild : CodeUpgradeBase
    {
        public override string Description => "Simplify DOCKERFILE removing duplicated dotnet build";

        public override void Execute(UpgradeContext uctx)
        {
            uctx.ChangeCodeFile(@"Southwind.React\Dockerfile", file =>
            {
                file.RemoveAllLines(a => a == @$"RUN dotnet build ""{uctx.ApplicationName}.React.csproj"" -c Release # -o /app/build");
            });
        }
    }
}
