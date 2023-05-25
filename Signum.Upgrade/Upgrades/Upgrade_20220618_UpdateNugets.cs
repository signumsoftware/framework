namespace Signum.Upgrade.Upgrades;

class Upgrade_20220618_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "Update Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReference("Microsoft.TypeScript.MSBuild", "4.7.4");
            file.UpdateNugetReference("Microsoft.Graph", "4.31.0");
            file.UpdateNugetReference("TensorFlow.Keras", "0.7.0");
        });
    }
}
