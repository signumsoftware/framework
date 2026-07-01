using Signum.Utilities;
using System;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20260610_ViteWatchIgnoreObjBin : CodeUpgradeBase
{
    public override string Description => "Add server.watch.ignored for obj/bin to avoid EBUSY on apphost.exe";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile(@"Southwind.Server/vite.config.js", file =>
        {
            file.InsertAfterFirstLine(a => a.Contains("origin"),
                """
                    watch: {
                        ignored: ['**/obj/**', '**/bin/**'],
                    },
                """);
        });
    }
}
