using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20240802_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "Update Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile("*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Azure.Messaging.ServiceBus" Version="7.18.1" />
                <PackageReference Include="DocumentFormat.OpenXml" Version="3.1.0" />
                <PackageReference Include="Azure.Storage.Blobs" Version="12.21.1" />
                <PackageReference Include="Selenium.Support" Version="4.23.0" />
                <PackageReference Include="Selenium.WebDriver" Version="4.23.0" />
                <PackageReference Include="System.Drawing.Common" Version="8.0.7" />
                <PackageReference Include="HtmlAgilityPack" Version="1.11.62" />
                <PackageReference Include="SixLabors.ImageSharp" Version="2.1.9" />
                <PackageReference Include="xunit" Version="2.9.0" /> 
                <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
                <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="8.0.2" />
                <PackageReference Include="Selenium.WebDriver.ChromeDriver" Version="127.0.6533.8800" />
                <PackageReference Include="Selenium.WebDriver" Version="4.23.0" />
                <PackageReference Include="Microsoft.TypeScript.MSBuild" Version="5.5.3">
                <PackageReference Include="Microsoft.VisualStudio.Azure.Containers.Tools.Targets" Version="1.21.0" />
                <PackageReference Include="Swashbuckle.AspNetCore" Version="6.7.0" />
                <PackageReference Include="Signum.TSGenerator" Version="8.5.1" />
                """);
        });
    }
}



