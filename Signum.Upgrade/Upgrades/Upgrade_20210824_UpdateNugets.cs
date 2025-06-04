namespace Signum.Upgrade.Upgrades;

class Upgrade_20210824_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "Upgrade some Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReference("Signum.Analyzer", "3.1.0");
            file.UpdateNugetReference("Microsoft.TypeScript.MSBuild", "4.3.5");
            file.UpdateNugetReference("Microsoft.VisualStudio.Azure.Containers.Tools.Targets", "1.11.1");
            file.UpdateNugetReference("Swashbuckle.AspNetCore", "6.1.5");
            file.UpdateNugetReference("SciSharp.TensorFlow.Redist", "2.6.0");
            file.UpdateNugetReference("Microsoft.NET.Test.Sdk", "16.11.0");
            file.UpdateNugetReference("DocumentFormat.OpenXml", "2.13.1");
        });
    }
}
