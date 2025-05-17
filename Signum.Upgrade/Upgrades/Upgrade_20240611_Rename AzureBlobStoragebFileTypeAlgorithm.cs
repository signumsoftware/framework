using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20240611_RenameAzureBlobStorage : CodeUpgradeBase
{
    public override string Description => "Rename AzureBlobStoragebFileTypeAlgorithm";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.cs", file =>
        {
            file.Replace("AzureBlobStoragebFileTypeAlgorithm", "AzureBlobStorageFileTypeAlgorithm");
        });
    }
}



