namespace Signum.Upgrade.Upgrades;

class Upgrade_20261119_MSBuildTask_TSGenerator : CodeUpgradeBase
{
    public override string Description => "Update Signum.MSBuildTask and Signum.TSGenerator to 11.0.0 (net11.0)";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Signum.MSBuildTask" Version="11.0.0" />
                <PackageReference Include="Signum.TSGenerator" Version="11.0.0" />
                """);
        });
    }
}
