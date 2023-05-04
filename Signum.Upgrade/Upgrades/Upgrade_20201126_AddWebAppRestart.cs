namespace Signum.Upgrade.Upgrades;

class Upgrade_20201126_AddWebAppRestart : CodeUpgradeBase
{
    public override string Description => "add az webapp restart to publichToAzure.ps1";


    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile("deployToAzure.ps1", file =>
        {
            file.InsertAfterFirstLine(a => a.Contains("docker push"),
@"az webapp restart --name southwind-webapp --resource-group southwind-resourceGroup");
        });
    }
}
