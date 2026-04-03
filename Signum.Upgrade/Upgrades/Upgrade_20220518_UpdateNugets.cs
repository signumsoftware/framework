namespace Signum.Upgrade.Upgrades;

class Upgrade_20220518_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "Update Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReference("Azure.Messaging.ServiceBus", "7.8.1");
            file.UpdateNugetReference("Azure.Storage.Blobs", "12.12.0");
            file.UpdateNugetReference("Microsoft.Graph", "4.28.0");
            file.UpdateNugetReference("Microsoft.Identity.Client", "4.43.2");
            file.UpdateNugetReference("DocumentFormat.OpenXml", "2.16.0");
            file.UpdateNugetReference("Microsoft.CodeAnalysis.CSharp", "4.2.0");
            file.UpdateNugetReference("SixLabors.ImageSharp", "2.1.1");
            file.UpdateNugetReference("Npgsql", "6.0.4");
            file.UpdateNugetReference("Selenium.Support", "4.1.1");
            file.UpdateNugetReference("Selenium.WebDriver", "4.1.1");
            file.UpdateNugetReference("System.IdentityModel.Tokens.Jwt", "6.17.0");
            file.UpdateNugetReference("Microsoft.IdentityModel.Protocols.OpenIdConnect", "6.17.0");
            file.UpdateNugetReference("Microsoft.IdentityModel.Tokens", "6.17.0");
            file.UpdateNugetReference("Microsoft.TypeScript.MSBuild", "4.6.4");
            file.UpdateNugetReference("Microsoft.Extensions.Configuration", "6.0.1");
            file.UpdateNugetReference("Microsoft.Extensions.Configuration.UserSecrets", "6.0.1");
            file.UpdateNugetReference("Microsoft.NET.Test.Sdk", "17.2.0");
            file.UpdateNugetReference("Selenium.WebDriver.ChromeDriver", "101.0.4951.4100");
            file.UpdateNugetReference("xunit.runner.visualstudio", "2.4.5");
        });
    }
}
