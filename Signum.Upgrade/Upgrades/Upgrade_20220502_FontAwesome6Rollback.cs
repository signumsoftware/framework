namespace Signum.Upgrade.Upgrades;

class Upgrade_20220502_FontAwesome6Rollback : CodeUpgradeBase
{
    public override string Description => "Rollback Font Awesome 6 upgrade";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile("Southwind.React/package.json", file =>
        {
            file.UpdateNpmPackages(@"""@fortawesome/fontawesome-svg-core"": ""1.3.0"",
    ""@fortawesome/free-brands-svg-icons"": ""5.15.4"",
    ""@fortawesome/free-regular-svg-icons"": ""5.15.4"",
    ""@fortawesome/free-solid-svg-icons"": ""5.15.4"",
");

        });

        uctx.ChangeCodeFile("Southwind.React/App/vendors.js", file =>
        {
            var packageJson = uctx.TryGetCodeFile("Southwind.React/package.json")!;

            file.InsertAfterFirstLine(a => a.Contains("react-router"), @"require(""react-router-dom"");");

            if (packageJson.Content.Contains("free-brands-svg-icons"))
                file.InsertAfterFirstLine(a => a.Contains("free-regular-svg-icons"), @"require(""@fortawesome/free-brands-svg-icons"");");

            if (packageJson.Content.Contains("@azure/msal-browser"))
                file.InsertAfterFirstLine(a => a.Contains("react-bootstrap"), @"require(""@azure/msal-browser"");");
        });
    }
}
