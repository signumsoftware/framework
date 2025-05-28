using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20230120_UpdatePackages : CodeUpgradeBase
{
    public override string Description => "Update Bootstrap";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile("Southwind.React/package.json", file =>
        {
            file.UpdateNpmPackages("""
                "@fortawesome/fontawesome-svg-core": "6.2.1",
                "@fortawesome/free-brands-svg-icons": "6.2.1",
                "@fortawesome/free-regular-svg-icons": "6.2.1",
                "@fortawesome/free-solid-svg-icons": "6.2.1",
                "@types/draft-js": "0.11.10",
                "@microsoft/signalr": "7.0.2",
                "@azure/msal-browser": "2.32.2",
                "codemirror": "6.65.7",
                "d3": "7.8.2",
                "@types/luxon": "3.2.0",
                "react-bootstrap": "2.7.0",
                """);
        });
    }
}



