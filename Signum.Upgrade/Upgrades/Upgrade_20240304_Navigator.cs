using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20240304_Navigator : CodeUpgradeBase
{
    public override string Description => "Navigator namespace";

    public override void Execute(UpgradeContext uctx)
    {
        var regexImport = new Regex(@"import\s+\*\s+as\s+Navigator\s+from", RegexOptions.Singleline);
        var regexHooksNavigator = new Regex(@"(?<!\.)(useFetchAndRemember|useFetchInState|useFetchInStateWithReload|useFetchAll|useLiteToString|useEntityChanged)\b", RegexOptions.Singleline);
        var navigatorHooks = "useFetchAndRemember|useFetchInState|useFetchInStateWithReload|useFetchAll|useLiteToString|useEntityChanged".Split("|");

        uctx.ForeachCodeFile(@"*.tsx", file =>
        {
            string? ReplaceIfNecessary(string from, string to)
            {
                if (!file.Content.Contains(from))
                    return null;

                file.Replace(from, to);
                return to;
            }

            var nvs = ReplaceIfNecessary("Navigator.NamedViewSettings", "NamedViewSettings");
            var vp = ReplaceIfNecessary("Navigator.ViewPromise", "ViewPromise");
            var es = ReplaceIfNecessary("Navigator.EntitySettings", "EntitySettings");

            file.ReplaceLine(a => a.StartsWith("import") && (a.EndsWith("/Navigator'")  || a.EndsWith("/Navigator\"")), line =>
            {
                var importPart = line.After("import").Before("from").Trim();

                var imports = importPart.Contains('*') ? new List<string> { "Navigator" } : importPart.Between("{", "}").SplitNoEmpty(",").Select(a => a.Trim()).ToList();

                imports.RemoveAll(navigatorHooks.Contains);

                imports.AddRange(new[] { nvs, vp, es }.NotNull());

                return "import { " + imports.ToString(", ") + " } from" + line.After("from"); 

            });

            //file.Replace(regexImport, $"import {{ {new[] { "Navigator", nvs, vp, es }.NotNull().ToString(", ")} }} from");

            file.Replace(regexHooksNavigator,  m => "Navigator." +  m.Value);

        });
    }
}
