using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20231206_VSCodeExtensions : CodeUpgradeBase
{
    public override string Description => "Update .vscode/extensions.json for C# Dev Kit";

    public override void Execute(UpgradeContext uctx)
    {


        uctx.ChangeCodeFile(@".vscode/extensions.json", file =>
        {
            file.ReplaceLine(a =>a.Contains("ms-vscode.csharp"), """
                "ms-dotnettools.csharp",
                "ms-dotnettools.csdevkit",
                "ms-dotnettools.vscode-dotnet-runtime",
                "ms-dotnettools.vscodeintellicode-csharp"
                """);

            file.RemoveAllLines(a => a.Contains("formulahendry.dotnet-test-explorer"));
        });
    }
}



