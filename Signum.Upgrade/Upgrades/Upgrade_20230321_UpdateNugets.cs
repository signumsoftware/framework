using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20230321_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "Updates Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                    <PackageReference Include="Azure.Messaging.ServiceBus" Version="7.13.1" />
                    <PackageReference Include="Azure.Storage.Blobs" Version="12.15.0" />
                    <PackageReference Include="Microsoft.Exchange.WebServices" Version="2.2.0" />
                    <PackageReference Include="Microsoft.Graph" Version="4.54.0" />
                    <PackageReference Include="Microsoft.Identity.Client" Version="4.51.0" />
                    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.5.0" />
                    <PackageReference Include="Selenium.Support" Version="4.8.1" />
                    <PackageReference Include="Selenium.WebDriver" Version="4.8.1" />
                    <PackageReference Include="Selenium.WebDriver.ChromeDriver" Version="111.0.5563.6400" />
                    <PackageReference Include="Microsoft.IdentityModel.Protocols.OpenIdConnect" Version="6.27.0" />
                    <PackageReference Include="Microsoft.IdentityModel.Tokens" Version="6.27.0" />
                    <PackageReference Include="Microsoft.TypeScript.MSBuild" Version="4.9.5">
                    <PackageReference Include="Npgsql" Version="7.0.2" />
                    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.5.0" />
                    <PackageReference Include="SkiaSharp.NativeAssets.Linux" Version="2.88.3" />
                    <PackageReference Include="Microsoft.VisualStudio.Azure.Containers.Tools.Targets" Version="1.18.1" />
                    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
                    <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="7.0.4" />
                    """);
        });
    }
}



