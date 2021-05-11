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
    class Upgrade_20210511_MSAL2 : CodeUpgradeBase
    {
        public override string Description => "Upgrade to MSAL 2.0 (@azure/msal-browser)";

        public override void Execute(UpgradeContext uctx)
        {

            uctx.ChangeCodeFile(@"Southwind.React/package.json", file =>
            {
                file.ReplaceLine(a=>a.Contains("msal"), @"""@azure/msal-browser"": ""2.14.1"",");
            });
        }
    }
}
