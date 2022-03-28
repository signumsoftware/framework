namespace Signum.Upgrade.Upgrades;

class Upgrade_20211027_UpdateNugetsSignumAnalyzer : CodeUpgradeBase
{
    public override string Description => "Update nugets, including Signum.Analyzer 3.2.0 preventing == between two entities or two lites";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReference("Azure.Storage.Files.Shares", "12.8.0");
            file.UpdateNugetReference("Microsoft.TypeScript.MSBuild", "4.4.4");
            file.UpdateNugetReference("Microsoft.NET.Test.Sdk", "17.0.0");
            file.UpdateNugetReference("Selenium.WebDriver", "4.0.1");
            file.UpdateNugetReference("Selenium.WebDriver.ChromeDriver", "95.0.4638.1700");
            file.ReplaceLine(a => a.Contains("Selenium.Chrome.WebDriver"), @"<PackageReference Include=""Selenium.WebDriver.ChromeDriver"" Version=""95.0.4638.1700"" />");
            file.UpdateNugetReference("Signum.Analyzer", "3.2.0");
            file.UpdateNugetReference("Swashbuckle.AspNetCore", "6.2.3");
        });
    }
}
