using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20231024_VisualTip : CodeUpgradeBase
{
    public override string Description => "Updates rename noOne to notAny and anyNo to notAll";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile(@"Southwind\Starter.cs", file =>
        {
            file.InsertAfterFirstLine(a => a.Contains("ExceptionLogic.Start(sb);"), 
                "VisualTipLogic.Start(sb);");

            file.InsertAfterFirstLine(a => a.Contains("sb.Schema.Settings.FieldAttributes((OperationLogEntity"), 
                "sb.Schema.Settings.FieldAttributes((VisualTipConsumedEntity ua) => ua.User).Replace(new ImplementedByAttribute(typeof(UserEntity)));");
        });

        uctx.ChangeCodeFile(@"Southwind\MainAdmin.tsx", file =>
        {
            file.InsertAfterFirstLine(a => a.Contains("import * as ExceptionClient"),
                @"import * as VisualTipClient from ""@framework/Basics/VisualTipClient""");

            file.InsertAfterFirstLine(a => a.Contains("ExceptionClient.start({ routes });"),
                "VisualTipClient.start({ routes });");
        });

    }
}



