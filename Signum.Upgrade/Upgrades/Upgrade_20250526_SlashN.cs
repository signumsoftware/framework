using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20250526_SlashRSlashN : CodeUpgradeBase
{
    public override string Description => "This replaces \r\n literals in code to \n";

    public override void Execute(UpgradeContext uctx)
    {
        SafeConsole.WriteLineColor(ConsoleColor.Red, @"Finally we will replace '\r\n' by '\n' in your C# and TypeScript code.");
        SafeConsole.WriteLineColor(ConsoleColor.Red, "Review with care!! This could introduce bugs.");

        uctx.ForeachCodeFile("*.tsx, *.ts, *.cs", c => c.Replace(@"\r\n", @"\n"));
    }
}



