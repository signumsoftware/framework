namespace Signum.Upgrade.Upgrades;

class Upgrade_20210108_RemoveLangVersion : CodeUpgradeBase
{
    public override string Description => "remove <LangVersion>";


    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.RemoveAllLines(a => a.Contains("<LangVersion>"));
        });
    }
}
