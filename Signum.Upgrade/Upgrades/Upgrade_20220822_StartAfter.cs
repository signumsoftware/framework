using System;
using System.IO;
using System.Net;
using System.Net.Http;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20220822_StartAfter : CodeUpgradeBase
{
    public override string Description => "Rename StartXXAfter methods";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile($@"Southwind.React\Startup.cs", file =>
        {
            file.Replace(
                "ProcessRunnerLogic.StartRunningProcesses(",
                "ProcessRunner.StartRunningProcessesAfter(");

            file.Replace(
                "SchedulerLogic.StartScheduledTasks();",
                "ScheduleTaskRunner.StartScheduledTaskAfter(5 * 1000);");

            file.Replace(
                "AsyncEmailSenderLogic.StartRunningEmailSenderAsync(",
                "AsyncEmailSender.StartAsyncEmailSenderAfter(");

        });
    }
}
