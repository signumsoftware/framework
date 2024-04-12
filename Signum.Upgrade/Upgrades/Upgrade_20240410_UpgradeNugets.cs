using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20240410_UpgradeNugets : CodeUpgradeBase
{
    public override string Description => "Upgrade nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile("*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Selenium.WebDriver.ChromeDriver" Version="123.0.6312.8600" />
                <PackageReference Include="Selenium.WebDriver" Version="4.19.0" />
                <PackageReference Include="Microsoft.TypeScript.MSBuild" Version="5.4.4"/>
                <PackageReference Include="Microsoft.VisualStudio.Azure.Containers.Tools.Targets" Version="1.20.1" />
                <PackageReference Include="Azure.Messaging.ServiceBus" Version="7.17.5" />
                <PackageReference Include="Selenium.Support" Version="4.19.0" />
                <PackageReference Include="Selenium.WebDriver" Version="4.19.0" />
                <PackageReference Include="System.Drawing.Common" Version="8.0.4" />
                <PackageReference Include="HtmlAgilityPack" Version="1.11.60" />
                """);
        });
    }
}



