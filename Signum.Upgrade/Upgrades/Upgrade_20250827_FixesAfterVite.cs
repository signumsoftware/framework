using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20250827_FixesAfterVite : CodeUpgradeBase
{
    public override string Description => "Fixes after Vite migration";

    public override void Execute(UpgradeContext uctx)
    {
        var port = Random.Shared.Next(3000, 3500);

        uctx.ChangeCodeFile(@".dockerignore", file =>
        {
            file.InsertAfterFirstLine(a => a.Contains("**/appsettings.*.json"),
                @"**/appsettings.json");
        });

        uctx.ChangeCodeFile(@"Southwind.Server/package.json", file =>
        {
            file.ReplaceLine(a => a.Contains("\"watch\""), "\"dev\"");
        });

        uctx.ChangeCodeFile(@"Southwind.Server/vite.config.js", file =>
        {
            int? port = null;
            file.ProcessLines(lines =>
            {
                var pLine = lines.SingleEx(a => a.Contains("port:"));
                port = pLine.After("port:").Before(",").ToInt();

                lines[lines.IndexOf(pLine)] = "port: port,".Indent(CodeFile.GetIndent(pLine));

                return true;
            });

            if (port.HasValue)
            {
                file.InsertBeforeFirstLine(a => a.Contains("export default defineConfig"), $"const port = {port!.Value};");
                file.InsertAfterLastLine(a => a.Contains("strictPort") || a.Contains("port: port"), $"origin: `http://localhost:${port}`,");
            }
        });
    }
}


