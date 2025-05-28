namespace Signum.Upgrade.Upgrades;

class Upgrade_20211112_FixBootstrap : CodeUpgradeBase
{
    public override string Description => "Set Production mode";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile($@"Southwind.React\Dockerfile", file =>
        {
            file.Replace("build-development", "build-production");
        });

        uctx.ChangeCodeFile($@"Southwind.React\Views\Home\Index.cshtml", file =>
        {
            file.Replace("supportIE = true", "supportIE = false");
        });

        uctx.ChangeCodeFile($@"Southwind.React\App\SCSS\custom.scss", file =>
        {
            file.InsertAfterFirstLine(a => a.Contains(@"@import ""./_bootswatch"";"), @".btn.input-group-text{
    background: $input-group-addon-bg;
    border: $input-border-width solid $input-group-addon-border-color
}");
        });

        uctx.ChangeCodeFile(@$"Southwind.React\package.json", file =>
        {

            file.Replace("--mode='production'", "--mode=production");
            file.Replace(
                @"webpack --config webpack.config.polyfills.js && webpack --config webpack.config.dll.js --mode=production",
                @"webpack --config webpack.config.polyfills.js --mode=production && webpack --config webpack.config.dll.js --mode=production");
        });
    }
}
