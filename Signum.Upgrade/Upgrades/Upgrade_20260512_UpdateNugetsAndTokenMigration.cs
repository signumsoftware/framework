namespace Signum.Upgrade.Upgrades;

class Upgrade_20260512_UpdateNugetsAndTokenMigration : CodeUpgradeBase
{
    public override string Description => "Update Nugets and add TokenMigrationLogic";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Microsoft.Graph" Version="5.105.0" />
                <PackageReference Include="Microsoft.Data.SqlClient" Version="7.0.1" />
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.5.1" />
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
