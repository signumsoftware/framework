using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Numerics;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20221220_FixDeployToAzure : CodeUpgradeBase
{
    public override string Description => "Fix Deploy to Azure (again)";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile($@"deploy*.ps1", file =>
        {
            file.Replace("Start-Process ", @"$p = (Start-Process ");
            file.Replace("-NoNewWindow -Wait", @"-NoNewWindow -Wait -PassThru)");
            file.Replace(
                """if(-Not $?){ Write-Host '"SQL Migrations" failed' -ForegroundColor DarkRed; exit; }""",
                """if($p.ExitCode -eq -1){ Write-Host '"SQL Migrations" failed' -ForegroundColor DarkRed; exit; }""");
        }, WarningLevel.Warning);
    }
}
