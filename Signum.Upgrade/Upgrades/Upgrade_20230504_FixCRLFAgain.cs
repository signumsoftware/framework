using Signum.Utilities;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20230504_FixCRLFAgain : CodeUpgradeBase
{
    public override string Description => "Fix line ends";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.t4s", file =>
        {
            file.Content = file.Content.Lines().ToString("\r\n");
        });

        uctx.ForeachCodeFile(@"*.ts", file =>
        {
            file.Content = file.Content.Lines().ToString("\r\n");
        });

        uctx.ForeachCodeFile(@"*.tsx", file =>
        {
            file.Content = file.Content.Lines().ToString("\r\n");
        });

        uctx.ForeachCodeFile(@"*.cs", file =>
        {
            file.Content = file.Content.Lines().ToString("\r\n");
        });

        uctx.ForeachCodeFile(@"*.cshtml", file =>
        {
            file.Content = file.Content.Lines().ToString("\r\n");
        });
    }
}
