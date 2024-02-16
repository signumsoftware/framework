using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20240215_GenericLines : CodeUpgradeBase
{
    public override string Description => "Remove EntityTable.typedColumns";

    public override void Execute(UpgradeContext uctx)
    {
        var regex = new Regex(@"EntityTable\.typedColumns<\w+>\((?<cols>\[.*?\])\)", RegexOptions.Singleline);

        uctx.ForeachCodeFile(@"*.tsx", file =>
        {
            file.Replace(regex, m => m.Groups["cols"].Value);
        });

        uctx.ForeachCodeFile("*.csproj", file =>
        {
            file.UpdateNugetReference("Microsoft.TypeScript.MSBuild", "5.4.0-beta");
        });

        uctx.ChangeCodeFile("Southwind.React/package.json", file =>
        {
            file.UpdateNpmPackage("typescript", "5.4.0-beta");
        });
    }
}



