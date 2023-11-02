using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20231102_NodeScript : CodeUpgradeBase
{
    public override string Description => "Update Node Script in Dockerfile";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile(@"Southwind/Dockerfile", file =>
        {
            file.ReplaceLine(l => l.Contains("curl -sL https://deb.nodesource.com/setup_1"), """
                RUN curl -SLO https://deb.nodesource.com/nsolid_setup_deb.sh
                RUN chmod 500 nsolid_setup_deb.sh
                RUN ./nsolid_setup_deb.sh 21
                """
                );
        });
    }
}



