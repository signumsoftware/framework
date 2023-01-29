using System;
using System.IO;
using System.Net;
using System.Net.Http;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20221004_DeployToAzure : CodeUpgradeBase
{
    public override string Description => "Fix Deploy to Azure";

    public override void Execute(UpgradeContext uctx)
    {
        Regex regexFormatText = new Regex(@"\blabelText\b");

        uctx.ForeachCodeFile($@"deploy*.ps1", file =>
        {
            file.InsertAfterFirstLine(a => a.StartsWith("az acr login"), @"if(-Not $?){ Write-Host '""az acr login"" failed' -ForegroundColor DarkRed; exit; }
");
            file.InsertAfterFirstLine(a => a.StartsWith("docker build"), @"if(-Not $?){ Write-Host '""docker build"" failed' -ForegroundColor DarkRed; exit; }
");
            file.InsertAfterFirstLine(a => a.StartsWith("docker push"), @"if(-Not $?){ Write-Host '""docker push"" failed' -ForegroundColor DarkRed; exit; }
");
            file.InsertAfterFirstLine(a => a.StartsWith("Start-Process"), @"if(-Not $?){ Write-Host '""SQL Migrations"" failed' -ForegroundColor DarkRed; exit; }
");
        });
    }
}
