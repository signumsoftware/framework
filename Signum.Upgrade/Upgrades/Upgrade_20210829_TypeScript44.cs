namespace Signum.Upgrade.Upgrades;

class Upgrade_20210829_TypeScript44 : CodeUpgradeBase
{
    public override string Description => "Update to TS 4.4";

    public override void Execute(UpgradeContext uctx)
    {
      
        uctx.ChangeCodeFile(@"Southwind.React/package.json", file =>
        {
            file.UpdateNpmPackage("ts-loader", "9.2.5");
            file.UpdateNpmPackage("typescript", "4.4.2");
        });

        uctx.ChangeCodeFile(@"Southwind.React/Southwind.React.csproj", file =>
        {
            file.UpdateNugetReference("Microsoft.TypeScript.MSBuild", "4.4.2");
        });
    }
}
