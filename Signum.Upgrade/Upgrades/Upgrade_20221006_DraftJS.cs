using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20221006_DraftJS : CodeUpgradeBase
{
    public override string Description => "Adds draftjs as a dependency for email templates";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile("package.json", file =>
        {
            file.InsertAfterLastLine(a => a.Contains("@fortawesome"), "\"@types/draft-js\": \"0.11.9\",");
            file.InsertBeforeFirstLine(a => a.Contains("\"history\""), "\"draft-js\": \"0.11.7\",");
            file.InsertBeforeFirstLine(a => a.Contains("\"history\""), "\"draftjs-to-html\": \"0.9.1\",");
            file.InsertAfterFirstLine(a => a.Contains("\"history\""), "\"html-to-draftjs\": \"1.5.0\",");
        });     
    }
}



