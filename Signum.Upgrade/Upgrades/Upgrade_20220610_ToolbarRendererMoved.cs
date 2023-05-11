namespace Signum.Upgrade.Upgrades;

class Upgrade_20220610_ToolbarRendererMoved : CodeUpgradeBase
{
    public override string Description => "ToolbarRenderer moved to Renderers";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile(@"Southwind.React\App\Layout.tsx", file =>
        {
            file.Replace("@extensions/Toolbar/Templates/ToolbarRenderer", "@extensions/Toolbar/Renderers/ToolbarRenderer");
        });
    }
}
