namespace Signum.Upgrade.Upgrades;

class Upgrade_20260512_UpdateNugetsAndTokenMigration : CodeUpgradeBase
{
    public override string Description => "Update Nugets and add TokenMigrationLogic";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Azure.Storage.Blobs" Version="12.28.0" />
                <PackageReference Include="Microsoft.Data.SqlClient" Version="7.0.1" />
                <PackageReference Include="Microsoft.Extensions.Configuration" Version="10.0.8" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="10.0.8" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="10.0.8" />
                <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="10.0.8" />
                <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.8" />
                <PackageReference Include="Microsoft.Graph" Version="5.105.0" />
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.5.1" />
                <PackageReference Include="System.DirectoryServices" Version="10.0.8" />
                <PackageReference Include="System.DirectoryServices.AccountManagement" Version="10.0.8" />
                <PackageReference Include="System.Drawing.Common" Version="10.0.8" />
                """);
        });

        uctx.ChangeCodeFile(@"Southwind/Starter.cs", file =>
        {
            file.InsertAfterFirstLine(a => a.Contains("using Signum.UserAssets;"),
                """
                using Signum.UserAssets.TokenMigrations;
                """);

            file.InsertAfterFirstLine(a => a.Contains("MigrationLogic.Start(sb);"),
                """
                TokenMigrationLogic.Start(sb);
                """);
        });
    }
}
