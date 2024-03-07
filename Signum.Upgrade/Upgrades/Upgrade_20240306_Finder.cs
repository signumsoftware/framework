using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Upgrade.Upgrades;
public class Upgrade_20240306_Finder : CodeUpgradeBase
{
    public override string Description => "Finder namespace";


    public override void Execute(UpgradeContext uctx)
    {
        var regexImport1 = new Regex(@"import\s+{\s+}\s+from\s+'@framework\/Finder'", RegexOptions.Singleline);
        var regexImport2 = new Regex(@"import\s+{\s+}\s+from\s+'.\/Finder'", RegexOptions.Singleline);
        var regexImport3 = new Regex(@"import\s+{\s+}\s+from\s+'..\/Finder'", RegexOptions.Singleline);
        var regexHooksFinder = new Regex(@"(?<!\.|get)(toFilterRequests|getTypeNiceName|renderFilterValue|CellFormatter|EntityFormatter|isAggregate|toFilterOptions|AddToLite|CellFormatterContext|EntityFormatRule|FilterValueFormatter|FormatRule|QuickFilterRule|filterValueFormatRules|decompress|formatRules|TokenCompleter|useInDB|parseOrderOptions)\b", RegexOptions.Singleline);
        var finderHooks = "toFilterRequests|getTypeNiceName|renderFilterValue|CellFormatter|EntityFormatter|isAggregate|toFilterOptions|AddToLite|CellFormatterContext|EntityFormatRule|FilterValueFormatter|FormatRule|QuickFilterRule|filterValueFormatRules|decompress|formatRules|TokenCompleter|useInDB|parseOrderOptions".Split("|");

        uctx.ForeachCodeFile(@"*.tsx, *.ts", file =>
        {
            if (file.FilePath.EndsWith("Finder.tsx"))
                return;

            file.ReplaceTypeScriptImports(path => path.EndsWith("/Finder"), parts =>
            {
                var ask = parts.Where(a => a.StartsWith("* as ")).ToList();
                if (ask.Any())
                {
                    ask.ForEach(a => parts.Remove(a));
                    parts.Add("Finder");
                }
                parts.RemoveWhere(finderHooks.Contains);

                return parts.OrderByDescending(a => a == "Finder").ToHashSet();
            });

            file.Replace(regexHooksFinder, m => "Finder." + m.Value);

            file.Replace(regexImport1, "import { Finder } from '@framework/Finder'");
            file.Replace(regexImport2, "import { Finder } from './Finder'");
            file.Replace(regexImport3, "import { Finder } from '../Finder'");
        });
    }
}
