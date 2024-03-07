using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20230912_isPermissionAuthorized : CodeUpgradeBase
{
    public override string Description => "Update isPermissionAuthorized";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.ts, *.tsx", file =>
        {
            file.Replace("AuthClient.isPermissionAuthorized", "isPermissionAuthorized");
            file.RemoveAllLines(a => a.Contains("import { isPermissionAuthorized } from '../Signum.Authorization/AuthClient';"));
            file.RemoveAllLines(a => a.Contains("import { isPermissionAuthorized } from '@extensions/Signum.Authorization/AuthClient';"));
            if(file.Content.Contains("isPermissionAuthorized"))
                file.InsertAfterLastLine(a => a.StartsWith("import"), "import { isPermissionAuthorized }from '@framework/AppContext'");
        });
    }
}
