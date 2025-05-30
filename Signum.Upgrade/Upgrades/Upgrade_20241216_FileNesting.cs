using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20241216_FileNesting : CodeUpgradeBase
{
    public override string Description => "Add .filenesting.json file to combine appconfig.json in Terminal and Tests";

    public override void Execute(UpgradeContext uctx)
    {
        var regex = new Regex("""\.GetAttribute\("(?<attr>[\w-]+)"\)""");
        uctx.CreateCodeFile(".filenesting.json", """
            {
                "help":"https://go.microsoft.com/fwlink/?linkid=866610",
            }
            """);
    }
}



