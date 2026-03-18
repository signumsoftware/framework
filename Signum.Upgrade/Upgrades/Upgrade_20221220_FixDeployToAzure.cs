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
