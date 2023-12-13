using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20231212_NugetUpdate : CodeUpgradeBase
{
    public override string Description => "Updates Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                    <PackageReference Include="System.DirectoryServices" Version="8.0.0" />
                    <PackageReference Include="System.DirectoryServices.AccountManagement" Version="8.0.0" />
                    <PackageReference Include="Microsoft.Graph" Version="5.36.0" />
                    <PackageReference Include="Microsoft.TypeScript.MSBuild" Version="5.3.3">
                    <PackageReference Include="DocumentFormat.OpenXml" Version="3.0.0" />
                    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.8.0" />
                    <PackageReference Include="DeepL.net" Version="1.8.0" />
                    <PackageReference Include="System.Drawing.Common" Version="8.0.0" />
                    <PackageReference Include="HtmlAgilityPack" Version="1.11.54" />
                    <PackageReference Include="Microsoft.Data.SqlClient" Version="5.1.2" />
                    <PackageReference Include="Npgsql" Version="8.0.1" />
                    <PackageReference Include="Selenium.Support" Version="4.16.2" />
                    <PackageReference Include="Selenium.WebDriver" Version="4.16.2" />
                    <PackageReference Include="SixLabors.ImageSharp" Version="2.1.6" />
                    <PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
                    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />
                    <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="8.0.0" />
                    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
                    <PackageReference Include="LibGit2Sharp" Version="0.29.0" />
                    <PackageReference Include="System.Text.Encoding.CodePages" Version="8.0.0" />
                    <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="8.0.0" />
                    <PackageReference Include="SciSharp.TensorFlow.Redist" Version="2.16.0" />
                    <PackageReference Include="xunit" Version="2.6.3" />
                    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.5">
                    <PackageReference Include="Selenium.WebDriver.ChromeDriver" Version="120.0.6099.7100" />
                    """);
        });
    }
}



