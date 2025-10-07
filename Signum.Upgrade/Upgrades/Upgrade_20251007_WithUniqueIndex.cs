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
            if (cf.FilePath.Contains(@"\Signum.Upgrade\"))
                return;

            // Apply patterns multiple times until no more changes (to handle multiple null checks)
            bool changed;
            do
            {
                var before = cf.Content;

                // Pattern 1: Remove "param.Property != null && " from predicates in WithUniqueIndex
                // Matches: .WithUniqueIndex(..., e => e.Column != null && e.DisableOrderIndex != true)
                cf.Replace(
                    new Regex(@"(\.WithUniqueIndex\s*\([^;]*,\s*)(\w+)\s*=>\s*\2\.(\w+)\s*!=\s*null\s*&&\s*"),
                    m => $"{m.Groups[1].Value}{m.Groups[2].Value} => "
                );

                // Pattern 2: Remove " && param.Property != null" from end of predicates in WithUniqueIndex
                cf.Replace(
                    new Regex(@"(\.WithUniqueIndex\s*\([^;]*,\s*\w+\s*=>\s*[^,)]+?)\s*&&\s*(\w+)\.(\w+)\s*!=\s*null(\s*[,\)])"),
                    m => $"{m.Groups[1].Value}{m.Groups[4].Value}"
                );

                // Pattern 3: Remove entire predicate if it's only "param.Property != null" in WithUniqueIndex
                // Matches: .WithUniqueIndex(..., a => a.LegacyId != null)
                cf.Replace(
                    new Regex(@"(\.WithUniqueIndex\s*\([^;]*),\s*(\w+)\s*=>\s*\2\.(\w+)\s*!=\s*null(\s*[,\)])"),
                    m => $"{m.Groups[1].Value}{m.Groups[4].Value}"
                );

                // Pattern 4: Clean up extra parentheses around simple boolean expressions in WithUniqueIndex
                cf.Replace(
                    new Regex(@"(\.WithUniqueIndex\s*\([^;]*,\s*)(\w+)\s*=>\s*\(\s*(\2\.\w+\s*(?:==|!=)\s*(?:true|false))\s*\)(\s*[,\)])"),
                    m => $"{m.Groups[1].Value}{m.Groups[2].Value} => {m.Groups[3].Value}{m.Groups[4].Value}"
                );

                // Pattern 5: Remove AllowMultipleNulls = true from UniqueIndex attribute (only parameter)
                // Matches: [UniqueIndex(AllowMultipleNulls = true)] or [UniqueIndex(AllowMultipleNulls = true), OtherAttribute]
                cf.Replace(
                    new Regex(@"\[UniqueIndex\s*\(\s*AllowMultipleNulls\s*=\s*true\s*\)"),
                    "[UniqueIndex"
                );

                // Pattern 6: Remove AllowMultipleNulls = true when it's the first parameter
                // Matches: [UniqueIndex(AllowMultipleNulls = true, ...)]
                cf.Replace(
                    new Regex(@"\[UniqueIndex\s*\(\s*AllowMultipleNulls\s*=\s*true\s*,\s*"),
                    "[UniqueIndex("
                );

                // Pattern 7: Remove AllowMultipleNulls = true when it's after other parameters
                // Matches: [UniqueIndex("Name", AllowMultipleNulls = true)]
                cf.Replace(
                    new Regex(@"(\[UniqueIndex\s*\([^)]+),\s*AllowMultipleNulls\s*=\s*true\s*\)"),
                    m => $"{m.Groups[1].Value})"
                );

                changed = cf.Content != before;
            } while (changed);
        });
    }
}