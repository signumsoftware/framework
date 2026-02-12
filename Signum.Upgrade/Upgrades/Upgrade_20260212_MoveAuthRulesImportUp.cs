using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20260212_MoveAuthRulesImportUp : CodeUpgradeBase
{
    public override string Description => "Move AuthRules Import in EnvironmentTest";

    public override void Execute(UpgradeContext uctx)
    {

        uctx.ChangeCodeFile(@"Southwind.Test.Environment\EnvironmentTest.cs", file =>
        {
            file.ProcessLines(lines =>
            {
                var index = lines.FindIndex(a => a.Contains("AuthLogic.ImportRulesScript"));
                if (index == -1)
                {
                    SafeConsole.WriteLineColor(ConsoleColor.Red, "AuthLogic.ImportRulesScript not found in EnvironmentTest.cs");
                    return false;
                }
                var importRulesLine = lines[index];
                lines.RemoveAt(index);

                var rolesIndex = lines.FindIndex(a => a.Contains("AuthLogic.LoadRoles"));
                if (rolesIndex == -1)
                {
                    SafeConsole.WriteLineColor(ConsoleColor.Red, "AuthLogic.LoadRoles not found in EnvironmentTest.cs");
                    return false;
                }

                lines.Insert(rolesIndex + 1, importRulesLine);
                return true;
            });
        });
    }
}
