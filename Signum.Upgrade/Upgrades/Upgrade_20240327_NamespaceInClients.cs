using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Signum.Upgrade.Upgrades;


class Upgrade_20240327_NamespaceInClients : CodeUpgradeBase
{
    public override string Description => "Namespace in *Client.tsx";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile("*Client.tsx, *Client.ts", file =>
        {
            file.ProcessLines(lines =>
            {
                var start = lines.FindLastIndex(a => a.StartsWith("import "));

                lines.Insert(start + 1, "");
                lines.Insert(start + 2, "export namespace " + Path.GetFileNameWithoutExtension(file.FilePath) + " {");

                var end = lines.FindLastIndex(a => a.HasText());

                lines.Insert(end + 1, "}");

                for (var i = start + 3; i <= end; i++)
                {
                    lines[i] = "  " + lines[i];
                }

                return true;
            });
        });

        uctx.ForeachCodeFile("*.tsx, *.ts", file =>
        {
            var dic = new Dictionary<string, string>();
            file.ReplacPartsInTypeScriptImport(a => a.EndsWith("Client"), (path, parts) =>
            {
                var newNameSpace = path.AfterLast("/");

                if (parts.Count == 1 && parts.SingleEx().StartsWith("* as "))
                {
                    var after = parts.SingleEx().After("* as ");
                    if (after != newNameSpace)
                        dic.Add(after, newNameSpace);

                    return new HashSet<string> { after };
                }
                else
                {
                    var simple = parts.Where(a => a == "API" || char.IsLower(a[0]));

                    dic.AddRange(simple.Select(p => KeyValuePair.Create(p, newNameSpace + "." + p)));

                    return [newNameSpace, ..parts.Except(simple)];
                }

            });

            foreach (var kvp in dic)
            {
                file.Replace(new Regex($@"\b(?<!\.){kvp.Key}\b"), kvp.Value);
            }
        });
    }
}



