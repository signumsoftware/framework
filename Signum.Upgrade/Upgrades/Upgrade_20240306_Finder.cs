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
        var simpleExports = "toFilterRequests|getTypeNiceName|renderFilterValue|CellFormatter|EntityFormatter|isAggregate|toFilterOptions|AddToLite|CellFormatterContext|EntityFormatRule|FilterValueFormatter|FormatRule|QuickFilterRule|filterValueFormatRules|decompress|formatRules|TokenCompleter|useInDB|useQuery|parseOrderOptions|parseFilterOptions|getResultTableTyped|fetchEntities|useInDBList|useFetchAllLite";
        var regexHooksFinder = new Regex($@"(?<!\.)\b({simpleExports})\b", RegexOptions.Singleline);
        var simpleExportArray = simpleExports.Split("|");

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
                if (parts.RemoveWhere(simpleExportArray.Contains) > 0)
                    parts.Add("Finder");

                return parts.OrderByDescending(a => a == "Finder").ToHashSet();
            });

            file.Replace(regexHooksFinder, m => "Finder." + m.Value);
        });
    }
}
