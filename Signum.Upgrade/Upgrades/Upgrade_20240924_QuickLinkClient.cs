using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20240924_QuickLinkClient : CodeUpgradeBase
{
    public override string Description => "Update Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        var regex = new Regex(@"new QuickLinks\.(?<linkType>\w+)");     
        uctx.ForeachCodeFile("*.tsx", file =>
        {
            HashSet<string> imports = new HashSet<string> { "QuickLinkClient" };
            file.Replace("QuickLinks.start", "QuickLinkClient.start");
            file.Replace("QuickLinks.registerQuickLink", "QuickLinkClient.registerQuickLink");
            file.Replace("QuickLinks.registerGlobalQuickLink", "QuickLinkClient.registerGlobalQuickLink");
            file.Replace(regex, m =>
            {
                var type = m.Groups["linkType"].Value;
                imports.Add(type);
                return $"new {type}";
            });
            file.Replace("import * as QuickLinks from '@framework/QuickLinks'", $"import {{ {imports.ToString(", ")} }} from '@framework/QuickLinkClient'");
            file.Replace(@"import * as QuickLinks from ""@framework/QuickLinks""", @$"import {{ {imports.ToString(", ")} }} from ""@framework/QuickLinkClient""");
        });
    }
}



