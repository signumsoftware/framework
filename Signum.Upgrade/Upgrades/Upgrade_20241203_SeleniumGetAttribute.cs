using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20241203_SeleniumGetAttribute : CodeUpgradeBase
{
    public override string Description => "Fix GetAttribute deprecation in Selenium tests";

    public override void Execute(UpgradeContext uctx)
    {
        var regex = new Regex("""\.GetAttribute\("(?<attr>[\w-]+)"\)""");
        uctx.ForeachCodeFile("*.cs", new[] { "Southwind.Test.React", "Framework\\Extensions\\Signum.Selenium" }, file =>
        {
            file.Replace(regex, a => a.Groups["attr"].Value.Let(a => a.StartsWith("data-") || a.StartsWith("aria-") ?
            $@".GetDomAttribute(""{a}"")" :
            $@".GetDomProperty(""{a}"")")); 
           


        });
    }
}



