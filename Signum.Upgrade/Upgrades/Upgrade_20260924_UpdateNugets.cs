namespace Signum.Upgrade.Upgrades;

class Upgrade_20260924_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "Update Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Microsoft.Extensions.Configuration" Version="10.0.12" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="10.0.12" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="10.0.12" />
                <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="10.0.12" />
                <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.12" />
                <PackageReference Include="System.DirectoryServices" Version="10.0.12" />
                <PackageReference Include="System.DirectoryServices.AccountManagement" Version="10.0.12" />
                <PackageReference Include="System.Drawing.Common" Version="10.0.12" />
                <PackageReference Include="Microsoft.Data.SqlClient" Version="7.1.0" />
                <PackageReference Include="Microsoft.Graph" Version="6.7.0" />
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.10.1" />
                <PackageReference Include="Microsoft.Playwright" Version="1.63.0" />
                <PackageReference Include="xunit.v3" Version="4.0.1" />
                <PackageReference Include="xunit.runner.visualstudio" Version="4.0.0" />
                <PackageReference Include="Swashbuckle.AspNetCore" Version="10.2.3" />
                <PackageReference Include="Azure.Messaging.ServiceBus" Version="7.20.2" />
                <PackageReference Include="Azure.Storage.Blobs" Version="12.29.2" />
                <PackageReference Include="DeepL.net" Version="1.22.1" />
                <PackageReference Include="HtmlAgilityPack" Version="1.13.0" />
                <PackageReference Include="Markdig" Version="1.4.0" />
                """);
        });
    }
}
