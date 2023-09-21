using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20230912_UpdateNugets7 : CodeUpgradeBase
{
    public override string Description => "Updates Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                    <PackageReference Include="System.DirectoryServices.AccountManagement" Version="7.0.1" />
                    <PackageReference Include="Microsoft.Graph" Version="5.28.0" />
                    <PackageReference Include="Azure.Storage.Blobs" Version="12.18.0" />
                    <PackageReference Include="Microsoft.Graph" Version="5.28.0" />
                    <PackageReference Include="Selenium.Support" Version="4.12.4" />
                    <PackageReference Include="Selenium.WebDriver" Version="4.12.4" />
                    <PackageReference Include="Selenium.WebDriver.ChromeDriver" Version="117.0.5938.8800" />
                    <PackageReference Include="Microsoft.TypeScript.MSBuild" Version="5.2.2"/>
                    <PackageReference Include="HtmlAgilityPack" Version="1.11.53" />
                    <PackageReference Include="xunit" Version="2.5.1" /> 
                    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.1">
                    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.7.2" />
                    <PackageReference Include="Npgsql" Version="7.0.6" />
                    """);
        });

        
    }
}



