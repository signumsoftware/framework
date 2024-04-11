using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signum.Upgrade.Upgrades;


class Upgrade_20240326_TypeScript_and_nugets2 : CodeUpgradeBase
{
    public override string Description => "New typescript 5.4.3 and other nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile("*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Microsoft.TypeScript.MSBuild" Version="5.4.3">
                <PackageReference Include="Microsoft.Graph" Version="5.45.0" />
                <PackageReference Include="DocumentFormat.OpenXml" Version="3.0.2" />
                <PackageReference Include="DeepL.net" Version="1.9.0" />
                <PackageReference Include="System.Drawing.Common" Version="8.0.3" />
                <PackageReference Include="DocumentFormat.OpenXml" Version="3.0.2" />
                <PackageReference Include="LibGit2Sharp" Version="0.30.0" />
                <PackageReference Include="Selenium.WebDriver.ChromeDriver" Version="123.0.6312.5800" />
                """);

            file.RemoveNugetReference("Microsoft.AspNetCore.ResponseCompression");
        });


    }
}



