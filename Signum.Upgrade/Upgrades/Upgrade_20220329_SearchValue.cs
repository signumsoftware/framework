namespace Signum.Upgrade.Upgrades;

class Upgrade_20220329_SearchValue : CodeUpgradeBase
{
    public override string Description => "Rename ValueSearchControl to SearchValue";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile("*.tsx", file =>
        {
            file.Replace(": ValueSearchControlLine", ": SearchValueLineController");
            file.Replace(": ValueSearchControl", ": SearchValueController");
            file.Replace("useRef<ValueSearchControlLine>", "useRef<SearchValueLineController>");
            file.Replace("useRef<ValueSearchControl>", "useRef<SearchValueController>");
            file.Replace("ValueSearchControlLine", "SearchValueLine");
            file.Replace("ValueSearchControl", "SearchValue");
        });
    }
}
