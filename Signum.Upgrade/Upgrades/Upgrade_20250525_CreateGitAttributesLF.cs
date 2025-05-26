using LibGit2Sharp;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20250525_CreateGitAttributesLF : CodeUpgradeBase
{
    public override string Description => ".gitattributes to use LN in all files";

    public override void Execute(UpgradeContext uctx)
    {
        SafeConsole.WriteLineColor(ConsoleColor.Yellow, "This upgrade will create .gitattributes with LF line endings for all files.");
        Console.WriteLine("Continue? (press any key)");
        Console.ReadLine();
        uctx.CreateCodeFile(@".gitattributes", "* text=auto eol=lf");

        SafeConsole.WriteLineColor(ConsoleColor.Yellow, "Now we will open a new git windows to renormalize all the files in the repo");
        var processInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/k \"cd /d \"{uctx.RootFolder}\" && git add --renormalize .\"",
            WindowStyle = ProcessWindowStyle.Normal,
            UseShellExecute = true // required to open a new window
        };

        Process.Start(processInfo);
    }
}



