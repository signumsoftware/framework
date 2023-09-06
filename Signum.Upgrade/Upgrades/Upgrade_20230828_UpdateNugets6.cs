using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20230828_UpdateNugets6 : CodeUpgradeBase
{
    public override string Description => "Updates Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                    <PackageReference Include="Microsoft.TypeScript.MSBuild" Version="5.2.2">
                    <PackageReference Include="Microsoft.Graph" Version="5.24.0" />
                    <PackageReference Include="Azure.Messaging.ServiceBus" Version="7.16.1" />
                    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.7.0" />
                    <PackageReference Include="Azure.Storage.Blobs" Version="12.17.0" />
                    <PackageReference Include="TensorFlow.Keras" Version="0.11.2" />
                    <PackageReference Include="Selenium.Support" Version="4.11.0" />
                    <PackageReference Include="Selenium.WebDriver" Version="4.11.0" />
                    <PackageReference Include="Selenium.WebDriver.ChromeDriver" Version="116.0.5845.9600" />
                    <PackageReference Include="HtmlAgilityPack" Version="1.11.52" />
                    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.7.1" />
                    <PackageReference Include="Microsoft.VisualStudio.Azure.Containers.Tools.Targets" Version="1.19.5" />
                    """);
        });

        
    }
}



