using LibGit2Sharp;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Signum.Upgrade.Upgrades;

class Upgrade_202304265_ProjectRevolution_updateSolution : CodeUpgradeBase
{
    public override string Description => "Project REVOLUTION - update solution";

    public override void Execute(UpgradeContext uctx)
    {
    }
}
