namespace Signum.Upgrade.Upgrades;

//https://techcommunity.microsoft.com/t5/sql-server-blog/released-general-availability-of-microsoft-data-sqlclient-4-0/ba-p/2983346
class Upgrade_20211219_MicrosoftDataSqlClient : CodeUpgradeBase
{
    public override string Description => "Microsoft.Data.SqlClient (Encrypt = true by default)";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReference("Microsoft.Data.SqlClient", "4.0.0");
            file.UpdateNugetReference("DocumentFormat.OpenXml", "2.14.0");
            file.UpdateNugetReference("Microsoft.Graph", "4.11.0");
        });

        uctx.ForeachCodeFile(@"*.json", file =>
        {
            file.Replace("Integrated Security=true", "Integrated Security=true;TrustServerCertificate=true");
        });
    }
}
