namespace Signum.Upgrade.Upgrades;

class Upgrade_20260422_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "Update Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Microsoft.Extensions.Configuration" Version="10.0.7" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="10.0.7" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="10.0.7" />
                <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="10.0.7" />
                <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.7" />
                <PackageReference Include="System.DirectoryServices" Version="10.0.7" />
                <PackageReference Include="System.DirectoryServices.AccountManagement" Version="10.0.7" />
                <PackageReference Include="System.Drawing.Common" Version="10.0.7" />
                <PackageReference Include="System.Text.Encoding.CodePages" Version="10.0.7" />
                <PackageReference Include="Azure.Identity" Version="1.21.0" />
                <PackageReference Include="DeepL.net" Version="1.21.0" />
                <PackageReference Include="Microsoft.Graph" Version="5.104.0" />
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.4.0" />
                <PackageReference Include="Microsoft.Playwright" Version="1.59.0" />
                """);
        });
    }
}
