using Signum.Utilities;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20251208_TypeScriptNativePreview : CodeUpgradeBase
{
    public override string Description => "Enable TypeScript Native Preview";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile("*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Signum.TSGenerator" Version="10.0.3" />
                <PackageReference Include="Microsoft.Graph" Version="5.98.0" />
                <PackageReference Include="Selenium.Support" Version="4.39.0" />
                <PackageReference Include="Selenium.WebDriver" Version="4.39.0" />
                <PackageReference Include="xunit.v3" Version="3.2.1" />
                <PackageReference Include="Selenium.WebDriver.ChromeDriver" Version="143.0.7499.4000" />
                """);
        });

        uctx.ChangeCodeFile("Southwind.Server/Southwind.Server.csproj", file =>
        {
            file.ReplaceLine(a => a.Contains("TSC_Build"), """
               <TSC_Build>tsgo</TSC_Build>
               """);
        });

        uctx.ChangeCodeFile("Southwind.Server/package.json", file =>
        {
            file.ReplaceLine(a => a.Contains("typescript"), """
               "@typescript/native-preview": "7.0.0-dev.20251206.1",
               """);
        });
    }
}
