using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Upgrade.Upgrades
{
    class Upgrade_20201125_ReactBootstrap14 : CodeUpgradeBase
    {
        public override string Description => "update react and react-bootstrap to 1.5";


        public override void Execute(UpgradeContext uctx)
        {
            uctx.ChangeCodeFile("Southwind.React/package.json", file =>
            {
                file.InsertAfterFirstLine(a => a.Contains("\"homepage\""),
@"""resolutions"": {
  ""@types/react"": ""file:../Framework/Signum.React/node_modules/@types/react""
},");

                file.UpdateNpmPackage("react", "16.14.0");
                file.UpdateNpmPackage("react-bootstrap", "1.4.0");
                file.UpdateNpmPackage("react", "16.14.0");
            });
        }
    }
}
