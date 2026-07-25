namespace Signum.Upgrade.Upgrades;

class Upgrade_20260710_TypeScript7Stable : CodeUpgradeBase
{
    public override string Description => "Switch from TypeScript Native Preview (tsgo) back to stable TypeScript 7.0.2";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile("Southwind.Server/Southwind.Server.csproj", file =>
        {
            file.ReplaceLine(a => a.Contains("TSC_Build"), """
               <TSC_Build>true</TSC_Build>
               """);
        });

        uctx.ChangeCodeFile("Southwind.Server/package.json", file =>
        {
            file.ReplaceLine(a => a.Contains("@typescript/native-preview"), """
               "typescript": "7.0.2",
               """);
        });
    }
}
