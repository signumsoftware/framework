namespace Signum.Upgrade.Upgrades;

class Upgrade_20211109_RemoveSerializable : CodeUpgradeBase
{
    public override string Description => "Remove Unnecessary [Serializable] attribute from entities";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.cs", uctx.EntitiesDirectory,  file =>
        {
            file.RemoveAllLines(a => a.Trim() == "[Serializable]");
            file.Replace(new Regex(@"\[\s*Serializable\s*\]\s*"), "");
            file.Replace(new Regex(@"Serializable\s*,\s*"), "");
        });
    }
}
