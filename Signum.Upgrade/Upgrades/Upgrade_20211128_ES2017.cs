namespace Signum.Upgrade.Upgrades;

class Upgrade_20211128_ES2017 : CodeUpgradeBase
{
    public override string Description => "Updage tsconfig to target 2017 (async await)";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile(@"Southwind.React\tsconfig.json", file =>
        {
            file.Replace(@"""target"": ""es5"",", @"""target"": ""ES2017"",");
        });
    }
}
