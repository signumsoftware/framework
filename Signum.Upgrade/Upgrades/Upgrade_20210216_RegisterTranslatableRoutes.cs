using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Upgrade.Upgrades
{
    class Upgrade_20210216_RegisterTranslatableRoutes : CodeUpgradeBase
    {
        public override string Description => "add instance translation to UserQuery / UserChart / Dashboard  / Toolbar";

        public override void Execute(UpgradeContext uctx)
        {
            uctx.ChangeCodeFile(@"Southwind.Logic/Starter.cs", file =>
            {
                file.InsertAfterFirstLine(line => line.Contains("UserQueryLogic.RegisterRoleTypeCondition"), "UserQueryLogic.RegisterTranslatableRoutes();");
                file.InsertAfterFirstLine(line => line.Contains("UserChartLogic.RegisterRoleTypeCondition"), "UserChartLogic.RegisterTranslatableRoutes();");
                file.InsertAfterFirstLine(line => line.Contains("DashboardLogic.RegisterRoleTypeCondition"), "DashboardLogic.RegisterTranslatableRoutes();");
                file.InsertAfterFirstLine(line => line.Contains("ToolbarLogic.Start(sb);"), "ToolbarLogic.RegisterTranslatableRoutes();");
                file.InsertAfterFirstLine(line => line.Contains("TranslationLogic.Start("), "TranslatedInstanceLogic.Start(sb, () => CultureInfo.GetCultureInfo(\"en\"));");
            });

        }
    }
}
