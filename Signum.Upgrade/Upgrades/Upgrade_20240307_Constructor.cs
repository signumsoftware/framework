using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Upgrade.Upgrades;
public class Upgrade_20240307_Constructor : CodeUpgradeBase
{
    public override string Description => "Constructor namespace";


    public override void Execute(UpgradeContext uctx)
    {
        var simpleExports = "Construct|clearCustomConstructors|registerConstructor";
        var regexHooksFinder = new Regex($@"(?<!\.)\b({simpleExports})\b", RegexOptions.Singleline);
        var simpleExportArray = simpleExports.Split("|");

        uctx.ForeachCodeFile(@"*.tsx, *.ts", file =>
        {
            if (file.FilePath.EndsWith("Constructor.tsx"))
                return;

            file.ReplaceAndCombineTypeScriptImports(path => path.EndsWith("/Constructor"), parts =>
            {
                var ask = parts.Where(a => a.StartsWith("* as ")).ToList();
                if (ask.Any())
                {
                    ask.ForEach(a => parts.Remove(a));
                    parts.Add("Constructor");
                }
                if (parts.RemoveWhere(simpleExportArray.Contains) > 0)
                    parts.Add("Constructor");

                return parts.OrderByDescending(a => a == "Constructor").ToHashSet();
            });

            file.Replace(regexHooksFinder, m => "Constructor." + m.Value);

        
        });
    }
}
