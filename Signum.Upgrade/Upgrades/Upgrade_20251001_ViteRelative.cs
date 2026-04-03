namespace Signum.Upgrade.Upgrades;

class Upgrade_20251001_ViteRelative : CodeUpgradeBase
{
    public override string Description => "Make vite use relative base address ''";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile(@"Southwind.Server/vite.config.js", cf => {
            cf.ReplaceLine(a => a.Contains("base:"), $"base: '', ");

        });

        uctx.ChangeCodeFile(@"Southwind.Server/Index.cshtml", cf => {
            cf.Replace("{vitePort}/dist/main.tsx", "{vitePort}/main.tsx");

        });
    }
}


