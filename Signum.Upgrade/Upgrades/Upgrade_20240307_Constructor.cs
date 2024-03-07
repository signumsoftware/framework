using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Upgrade.Upgrades;
public class Upgrade_20240307_Constructor : CodeUpgradeBase
{
    public override string Description => "Constructor namespace";


    public override void Execute(UpgradeContext uctx)
    {
        var regexImport1 = new Regex(@"import\s+{\s+}\s+from\s+'@framework\/Constructor'", RegexOptions.Singleline);
        var regexImport2 = new Regex(@"import\s+{\s+}\s+from\s+'.\/Constructor'", RegexOptions.Singleline);
        var regexImport3 = new Regex(@"import\s+{\s+}\s+from\s+'..\/Constructor'", RegexOptions.Singleline);
        var regexHooksFinder = new Regex(@"(?<!\.|on|default|can)(Construct|clearCustomConstructors|registerConstructor)\b", RegexOptions.Singleline);
        var finderHooks = "Construct|clearCustomConstructors|registerConstructor".Split("|");

        uctx.ForeachCodeFile(@"*.tsx, *.ts", file =>
        {
            if (file.FilePath.EndsWith("Constructor.tsx"))
                return;

            file.ReplaceTypeScriptImports(path => path.EndsWith("/Constructor"), parts =>
            {
                var ask = parts.Where(a => a.StartsWith("* as ")).ToList();
                if (ask.Any())
                {
                    ask.ForEach(a => parts.Remove(a));
                    parts.Add("Constructor");
                }
               parts.RemoveWhere(finderHooks.Contains);

                return parts.OrderByDescending(a => a == "Constructor").ToHashSet();
            });

            file.Replace(regexHooksFinder, m => "Constructor." + m.Value);

            file.Replace(regexImport1, "import { Constructor } from '@framework/Constructor'");
            file.Replace(regexImport2, "import { Constructor } from './Constructor'");
            file.Replace(regexImport3, "import { Constructor } from '../Constructor'");
        });
    }
}
