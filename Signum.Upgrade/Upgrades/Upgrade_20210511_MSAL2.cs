namespace Signum.Upgrade.Upgrades;

class Upgrade_20210511_MSAL2 : CodeUpgradeBase
{
    public override string Description => "Upgrade to MSAL 2.0 (@azure/msal-browser)";

    public override void Execute(UpgradeContext uctx)
    {

        uctx.ChangeCodeFile(@"Southwind.React/package.json", file =>
        {
            file.ReplaceLine(a=>a.Contains("msal"), @"""@azure/msal-browser"": ""2.14.1"",");
        });
    }
}
