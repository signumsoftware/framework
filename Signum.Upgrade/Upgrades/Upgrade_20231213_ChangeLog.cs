using Signum.Utilities;
using System;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20231213_ChangeLog : CodeUpgradeBase
{
    public override string Description => "Add ChangeLog";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.CreateCodeFile(@"Southwind/Changelog.ts", $$"""
            import type { ChangeLogDic } from "@framework/Basics/ChangeLogClient";

            export default {
              "{{DateTime.Today.ToDateOnly().ToIsoString()}}": [
                "Add ChangeLog",
                "Update Signum",
              ],
            } satisfies ChangeLogDic;
            """);

        uctx.ChangeCodeFile(@"Southwind/Layout.tsx", c =>
        {
            c.InsertAfterFirstLine(a => a.Contains("const OmniboxAutocomplete"),
                """const ChangeLogViewer = React.lazy(() => import('@framework/Basics/ChangeLogViewer'));""");

            c.ReplaceLine(a => a.Contains("<VersionInfo"), @"<React.Suspense fallback={null}><ChangeLogViewer extraInformation={(window as any).__serverName} /></React.Suspense>");
        });

        uctx.ChangeCodeFile(@"Southwind/MainAdmin.tsx", c =>
        {
            c.InsertAfterFirstLine(a => a.Contains("import * as ExceptionClient"),
                """
                import * as ChangeLogClient from "@framework/Basics/ChangeLogClient"
                """);

            c.InsertAfterFirstLine(a => a.Contains("ExceptionClient.start"),
                $$"""
                ChangeLogClient.start({ routes, applicationName: "{{uctx.ApplicationName}}", mainChangeLog: () => import("./Changelog") });
                """);
        });

        uctx.ChangeCodeFile(@"Southwind/Starter.cs", c =>
        {
            c.InsertAfterFirstLine(a => a.Contains("ExceptionLogic.Start(sb);"),
                """
                ChangeLogLogic.Start(sb);
                """);

            c.InsertAfterFirstLine(a => a.Contains("sb.Schema.Settings.FieldAttributes((OperationLogEntity"),
                """
                sb.Schema.Settings.FieldAttributes((ChangeLogViewLogEntity cl) => cl.User).Replace(new ImplementedByAttribute(typeof(UserEntity)));
                """);
        });
    }
}



