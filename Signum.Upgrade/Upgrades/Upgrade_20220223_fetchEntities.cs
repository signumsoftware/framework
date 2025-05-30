using Signum.Utilities;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20220223_fetchEntities : CodeUpgradeBase
{
    public override string Description => "Replaces fetchLitesWithFilters to fetchLites";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.tsx, *.ts", file =>
        {
            file.Replace(new Regex(@"fetch(?<typ>Entities|Lites)WithFilters\(\s*(?<qn>\w+)\s*,\s*(?<filter>\[[^]]*\])\s*,\s*(?<orders>\[[^]]*\])\s*,\s*(?<count>(\d+|null))\s*\)"), m =>
            {
                return @$"fetch{m.Groups["typ"]}({{
    queryName: {m.Groups["qn"]},
    filterOptions: {m.Groups["filter"]},
    orderOptions: {m.Groups["orders"]},
    count: {m.Groups["count"]},
}})";
            });
        });
    }
}
