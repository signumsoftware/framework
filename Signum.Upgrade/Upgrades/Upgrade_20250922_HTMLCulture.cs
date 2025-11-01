using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20250922_HTMLCulture : CodeUpgradeBase
{
    public override string Description => "Fixes after WCAG changes";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile(@"Southwind/Index.cshtml", file =>
        {
            file.Replace("<html lang=\"en\">", "<html lang=\"@CultureInfo.CurrentUICulture.Name\">");
            file.Replace("<html>", "<html lang=\"@CultureInfo.CurrentUICulture.Name\">");

            file.InsertAfterLastLine(line => line.Contains("@using"), "@using System.Globalization");
        });
    }
}


