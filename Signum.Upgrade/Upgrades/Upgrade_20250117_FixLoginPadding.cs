using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20250117_FixLoginPadding : CodeUpgradeBase
{
    public override string Description => "";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile("Southwind/Layout.tsx", a =>
        {
            a.Replace("<div className=\"navbar-nav ml-auto\">", "<div className=\"navbar-nav ml-auto me-2\">");
        });
    }
}



