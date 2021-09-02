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
    class Upgrade_20210828_ExtensionsLoveFramework : CodeUpgradeBase
    {
        public override string Description => "Extensions ❤️ Framework";

        public override void Execute(UpgradeContext uctx)
        {
            uctx.ForeachCodeFile(@"*.csproj", file =>
            {
                file.Replace(@"\Extensions\", @"\Framework\");
            });

            uctx.ForeachCodeFile(@"*.sln", file =>
            {
                file.Replace(@"""Extensions\", @"""Framework\");
            });

            uctx.ChangeCodeFile(@"Southwind.React\webpack.config.js", file =>
            {
                file.Replace(@"/Extensions/", @"/Framework/");
            });

            uctx.ChangeCodeFile(@"Southwind.React/tsconfig.json", file =>
            {
                file.Replace(@"/Extensions/", @"/Framework/");
            });

            uctx.ChangeCodeFile(@"Southwind.React/package.json", file =>
            {
                file.Replace(@"/Extensions/", @"/Framework/");
            });

            uctx.ChangeCodeFile(@"Southwind.React/Dockerfile", file =>
            {
                file.Replace(@"""Extensions/", @"""Framework/");
            });

            uctx.ChangeCodeFile(@".gitmodules", file =>
            {
                file.RemoveAllLines(a => a.Contains("extensions", StringComparison.InvariantCultureIgnoreCase));
            });
            
            if (SafeConsole.Ask("Do you want to delete 'Extensions' folder with all his content?"))
            {
                Directory.Delete(Path.Combine(uctx.RootFolder, "Extensions"), true);
                Console.WriteLine("deleted");
            }
        }
    }
}
