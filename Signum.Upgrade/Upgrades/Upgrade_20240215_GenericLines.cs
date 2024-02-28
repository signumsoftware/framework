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
        var regex2 = new Regex(@"getComponent={\((?<ident>\w+)\s*:\s*TypeContext<\w+>\)", RegexOptions.Singleline);

        uctx.ForeachCodeFile(@"*.tsx", file =>
        {
            file.Replace(regex, m => m.Groups["cols"].Value);
            file.Replace(regex2, m => "getComponent={" + m.Groups["ident"].Value);
        });

        uctx.ForeachCodeFile("*.csproj", file =>
        {
            file.UpdateNugetReference("Microsoft.TypeScript.MSBuild", "5.4.0-beta");
        });

        uctx.ChangeCodeFile("Southwind/package.json", file =>
        {
            file.UpdateNpmPackage("typescript", "5.4.0-beta");
        });
    }
}



