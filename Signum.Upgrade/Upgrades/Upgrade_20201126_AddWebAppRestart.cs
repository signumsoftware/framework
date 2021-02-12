using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Upgrade.Upgrades
{
    class Upgrade_20201126_AddWebAppRestart : CodeUpgradeBase
    {
        public override string Description => "add az webapp restart to publichToAzure.ps1";


        public override void Execute(UpgradeContext uctx)
        {
            uctx.ChangeCodeFile("deployToAzure.ps1", file =>
            {
                file.InsertAfterFirstLine(a => a.Contains("docker push"),
@"az webapp restart --name southwind-webapp --resource-group southwind-resourceGroup");
            });
        }
    }
}
