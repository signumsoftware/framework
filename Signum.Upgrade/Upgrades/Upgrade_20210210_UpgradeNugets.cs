namespace Signum.Upgrade.Upgrades;

class Upgrade_20210210_UpgradeNugets : CodeUpgradeBase
{
    public override string Description => "Upgrade a few nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile(@"Southwind.React\Southwind.React.csproj", file =>
        {
            file.UpdateNugetReference("Microsoft.TypeScript.MSBuild", "4.1.4");
            file.WarningLevel = WarningLevel.Warning;
            file.UpdateNugetReference("Swashbuckle.AspNetCore", "6.0.3");
        });

    }
}
