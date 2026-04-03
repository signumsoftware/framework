namespace Signum.Upgrade.Upgrades;

class Upgrade_20210924_RemoveParentTokenParentValue : CodeUpgradeBase
{
    public override string Description => "Remove parentToken / parentValue from FindOptions";

    public override void Execute(UpgradeContext uctx)
    {
        Regex regex = new Regex(@"parentToken\s*:\s*(?<parentToken>[^,]+),(\s|\n)+parentValue\s*:\s*(?<parentValue>[^,}\r\n]+)");

        uctx.ForeachCodeFile(@"*.ts, *.tsx", file =>
        {
            file.Replace(regex, r => $"filterOptions: [{{ token: {r.Groups["parentToken"]}, value: {r.Groups["parentValue"]}}}] ");
        });
    }
}
