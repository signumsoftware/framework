using LibGit2Sharp;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
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
        var simpleExports = "useFetchAndRemember|useFetchInState|useFetchInStateWithReload|useFetchAll|useLiteToString|useEntityChanged";

        var regexSipleNavigator = new Regex($@"(?<!\.)({simpleExports})\b", RegexOptions.Singleline);
        var simpleExportsArray = simpleExports.Split("|");

        

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
            var enc = ReplaceIfNecessary("Navigator.EnumConverter", "EnumConverter");

            file.ReplaceTypeScriptImports(path => path.EndsWith("/Navigator"), parts =>
            {
                var ask = parts.Where(a => a.StartsWith("* as ")).ToList();
                if (ask.Any())
                {
                    ask.ForEach(a => parts.Remove(a));
                    parts.Add("Navigator");
                }


                parts.AddRange(new[] { nvs, vp, es, enc }.NotNull());

                if (parts.RemoveWhere(simpleExportsArray.Contains) > 0)
                    parts.Add("Navigator");

                return parts.OrderByDescending(a => a == "Navigator").ToHashSet();
            });


            file.Replace(regexSipleNavigator,  m => "Navigator." +  m.Value);

        });
    }
}
