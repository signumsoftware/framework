namespace Signum.Upgrade.Upgrades;

class Upgrade_20210726_SimplifyDockerBuild : CodeUpgradeBase
{
    public override string Description => "Simplify DOCKERFILE removing duplicated dotnet build";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile(@"Southwind.React\Dockerfile", file =>
        {
            file.RemoveAllLines(a => a == @$"RUN dotnet build ""{uctx.ApplicationName}.React.csproj"" -c Release # -o /app/build");
        });
    }
}
