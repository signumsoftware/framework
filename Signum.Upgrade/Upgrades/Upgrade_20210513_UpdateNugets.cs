namespace Signum.Upgrade.Upgrades;

class Upgrade_20210513_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "Upgrade some Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReference("Swashbuckle.AspNetCore", "6.1.4");
            file.UpdateNugetReference("SciSharp.TensorFlow.Redist", "2.4.1");
            file.UpdateNugetReference("Microsoft.NET.Test.Sdk", "16.9.4");
        });
    }
}
