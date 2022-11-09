using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20221109_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "Updates Nuget";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
            <PackageReference Include="Microsoft.VisualStudio.Azure.Containers.Tools.Targets" Version="1.17.0" />
            <PackageReference Include="Microsoft.Extensions.Configuration" Version="7.0.0" />
            <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="7.0.0" />
            <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="7.0.0" />
            <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="7.0.0" />
            <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.4.0" />
            <PackageReference Include="Selenium.WebDriver.ChromeDriver" Version="107.0.5304.6200" />
            <PackageReference Include="Selenium.WebDriver" Version="4.6.0" />
            <PackageReference Include="Selenium.Support" Version="4.6.0" />
            <PackageReference Include="Azure.Messaging.ServiceBus" Version="7.11.1" />
            <PackageReference Include="Azure.Storage.Blobs" Version="12.14.1" />
            <PackageReference Include="DeepL" Version="0.4.2" />
            <PackageReference Include="Microsoft.Graph" Version="4.47.0" />
            <PackageReference Include="Microsoft.Identity.Client" Version="4.48.0" />
            <PackageReference Include="System.DirectoryServices" Version="7.0.0" />
            <PackageReference Include="System.DirectoryServices.AccountManagement" Version="7.0.0" />
            <PackageReference Include="System.Drawing.Common" Version="7.0.0" />
            <PackageReference Include="Microsoft.Data.SqlClient" Version="5.0.1" />
            <PackageReference Include="Microsoft.IdentityModel.Protocols.OpenIdConnect" Version="6.25.0" />
            <PackageReference Include="Microsoft.IdentityModel.Tokens" Version="6.25.0" />
            <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="6.25.0" />
            <PackageReference Include="System.Text.Encoding.CodePages" Version="7.0.0" />
            """);
        });
    }
}



