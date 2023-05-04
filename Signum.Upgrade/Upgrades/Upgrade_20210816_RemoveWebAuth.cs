namespace Signum.Upgrade.Upgrades;

class Upgrade_20210816_RemoveWebAuth : CodeUpgradeBase
{
    public override string Description => "Remove WebAuthn";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile(@"Southwind.Test.Environment/SouthwindEnvironment.cs", file =>
        {
            file.ReplaceBetween(a => a.Contains("WebAuthn = new WebAuthnConfigurationEmbedded"), -1, a => a.Contains("}, //Auth"), -0, "");
        });

        uctx.ChangeCodeFile(@"Southwind.Terminal/SouthwindMigrations.cs", file =>
        {
            file.ReplaceBetween(a => a.Contains("WebAuthn = new WebAuthnConfigurationEmbedded"), -1, a => a.Contains("}, //Auth"), -0, "");
        });

        uctx.ChangeCodeFile(@"Southwind.React/App/Southwind/Templates/ApplicationConfiguration.tsx", file =>
        {
            file.ReplaceBetween(a => a.Contains(@"<Tab eventKey=""webauthn"" title={ctx.niceName(a => a.webAuthn)}>"), -1, a => a.Contains(@"</Tab>"), -0, "");
        });

        uctx.ChangeCodeFile(@"Southwind.React/App/MainPublic.tsx", file =>
        {
            file.RemoveAllLines(a => a.Contains(@"WebAuthnClient"));
        });

        uctx.ChangeCodeFile(@"Southwind.React/App/Layout.tsx", file =>
        {
            file.RemoveAllLines(a => a.Contains(@"@extensions/Authorization/WebAuthn/WebAuthnClient"));
            file.Replace("/*extraButons={user => <WebAuthnClient.WebAuthnRegisterMenuItem />}*/", "");
        });

        uctx.ChangeCodeFile(@"Southwind.Logic/Starter.cs", file =>
        {
            file.RemoveAllLines(a => a.Contains(@"WebAuthnLogic.Start(sb, ()=> Configuration.Value.WebAuthn);"));
        });

        uctx.ChangeCodeFile(@"Southwind.Entities/ApplicationConfiguration.cs", file =>
        {
            file.RemoveAllLines(a => a.Contains(@"public WebAuthnConfigurationEmbedded WebAuthn { get; set; }"));
        });
    }
}
