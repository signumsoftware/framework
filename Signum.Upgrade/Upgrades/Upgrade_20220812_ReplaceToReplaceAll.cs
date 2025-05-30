namespace Signum.Upgrade.Upgrades;

class Upgrade_20220812_ReplaceToReplaceAll : CodeUpgradeBase
{
    public override string Description => "columnOptionMode: 'Replace' -> 'ReplaceAll'";

    public static Regex ColumnOptionModeRegex = new Regex(@"columnOptionsMode\s*:\s*['""]Replace['""]");
    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.tsx, *.ts", file =>
        {
            file.Replace(ColumnOptionModeRegex, m => m.Value.Replace("Replace", "ReplaceAll"));
        });

        uctx.ForeachCodeFile("*.csproj", file =>
        {
            file.UpdateNugetReference("Microsoft.Graph", "4.36.0");
            file.UpdateNugetReference("Microsoft.Identity.Client", "4.46.0");
            file.UpdateNugetReference("Microsoft.NET.Test.Sdk", "17.3.0");
            file.UpdateNugetReference("xunit", "2.4.2");
            file.UpdateNugetReference("Selenium.WebDriver.ChromeDriver", "104.0.5112.7900");
            file.UpdateNugetReference("Selenium.WebDriver", "4.4.0");
            file.UpdateNugetReference("Selenium.Suppor", "4.4.0");
            file.UpdateNugetReference("Npgsql", "6.0.6");
            file.UpdateNugetReference("Microsoft.VisualStudio.Azure.Containers.Tools.Targets", "1.16.1");
            file.UpdateNugetReference("Swashbuckle.AspNetCore", "6.4.0");
            file.UpdateNugetReference("Unofficial.Microsoft.SqlServer.Types", "5.0.0");
            file.UpdateNugetReference("Microsoft.Data.SqlClient", "5.0.0");
            file.UpdateNugetReference("DocumentFormat.OpenXml", "2.17.1");
            file.UpdateNugetReference("Azure.Storage.Blobs", "12.13.0");
        });
    }
}
