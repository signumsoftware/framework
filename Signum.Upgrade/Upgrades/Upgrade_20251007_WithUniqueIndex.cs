using System;
using System.Text.RegularExpressions;
using Signum.Utilities;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20251007_WithUniqueIndex : CodeUpgradeBase
{
    public override string Description => "Remove not null condition from WithUniqueIndex and remove AllowMultipleNulls";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile("*.cs", cf =>
        {
            cf.Replace(
                new Regex(@".WithUniqueIndex\(\s*(?<pCols>\w+)\s*=> *(?<columns>.*?),\s*(?<pPred>\w+)\s*=> *(?<predicate>.*?)(?<end>[\),])"),
                m =>
                {
                    var pCols = m.Groups["pCols"].Value;
                    var columns = m.Groups["columns"].Value;
                    var pPred = m.Groups["pPred"].Value;
                    var predicate = m.Groups["predicate"].Value;
                    var end = m.Groups["end"].Value;
                    var members = Regex.Matches(m.Groups["columns"].Value, pCols + @"+(\.\w+)+").Cast<Match>();

                    foreach (var me in members)
                    {
                        predicate = Regex.Replace(predicate, Regex.Escape(me.Value) + @"\s*!=\s*null", "");
                    }

                    predicate = Regex.Replace(predicate, @"^[&\s+]+", "");
                    predicate = Regex.Replace(predicate, @"[&\s+]+$", "");
                    predicate = Regex.Replace(predicate, @"\s+(&&[\s+])+", " && ");

                    if (predicate.HasText())
                        return $".WithUniqueIndex({pCols} => {columns}, {pPred} => {predicate}{end}";
                    else
                        return $".WithUniqueIndex({pCols} => {columns}{end}";
                }
            );

            cf.Replace(
                new Regex(@"\[UniqueIndex\s*\(\s*AllowMultipleNulls\s*=\s*true\s*\)"),
                "[UniqueIndex"
            );

            cf.Replace(
                new Regex(@"\[UniqueIndex\s*\(\s*AllowMultipleNulls\s*=\s*true\s*,\s*"),
                "[UniqueIndex("
            );

            cf.Replace(
                new Regex(@"(?<main>\[UniqueIndex\s*\([^)]+),\s*AllowMultipleNulls\s*=\s*true\s*\)"),
                m => $"{m.Groups["main"].Value})"
            );
        });
    }
}
