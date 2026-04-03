using Signum.Utilities;
using System;
using System.IO;

namespace Signum.Upgrade.Upgrades;

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

        uctx.ForeachCodeFile(@"*.ts, *.tsx", file =>
        {
            file.Replace(@"../../../../Extensions/Signum.React.Extensions/", @"@extensions/");
            file.Replace(@"../../../Extensions/Signum.React.Extensions/", @"@extensions/");
            file.Replace(@"../../Extensions/Signum.React.Extensions/", @"@extensions/");

            file.Replace(@"../../../../Framework/Signum.React/Scripts/", @"@framework/");
            file.Replace(@"../../../Framework/Signum.React/Scripts/", @"@framework/");
            file.Replace(@"../../Framework/Signum.React/Scripts/", @"@framework/");

            
        });

        if (SafeConsole.Ask("Do you want to delete 'Extensions' folder with all his content?"))
        {
            Directory.Delete(Path.Combine(uctx.RootFolder, "Extensions"), true);
            Console.WriteLine("deleted");
        }
    }
}
