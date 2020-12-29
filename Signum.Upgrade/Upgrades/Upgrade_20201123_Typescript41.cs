using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Upgrade.Upgrades
{
    class Upgrade_20201123_Typescript41 : CodeUpgradeBase
    {
        public override string Description => "Update to Typescript 4.1";


        public override void Execute(UpgradeContext uctx)
        {
            uctx.ChangeCodeFile("Southwind.React/package.json", file =>
            {
                file.UpdateNpmPackage("ts-loader", "8.0.11");
                file.UpdateNpmPackage("typescript", "4.1.2");
            });

            uctx.ChangeCodeFile("Southwind.React/Southwind.React.csproj", file =>
            {
                file.UpdateNugetReference("Microsoft.TypeScript.MSBuild", "4.1.2");
            });

            uctx.ChangeCodeFile("Southwind.React/tsconfig.json", file =>
            {
                file.RemoveAllLines(a => a.Contains(@"""baseUrl"": ""."","));
                file.Replace("\"*\": [", "temp123");
                file.Replace("\"*\"", "\"./*\"");
                file.Replace("temp123", "\"*\": [");

            });
        }
    }
}
