using Signum.CodeGeneration;
using Signum.Utilities;
using System;

namespace Signum.Upgrade;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine();
        Console.WriteLine("  ..:: Welcome to Signum Upgrade ::..");
        Console.WriteLine();

        SafeConsole.WriteLineColor(ConsoleColor.DarkGray, "  This application helps you upgrade a Signum Framework application by modifying your source code.");
        SafeConsole.WriteLineColor(ConsoleColor.DarkGray, "  The closer your application resembles Southwind, the better it works.");
        SafeConsole.WriteLineColor(ConsoleColor.DarkGray, "  Review all the changes carefully");
        Console.WriteLine();

        var uctx = UpgradeContext.CreateFromCurrentDirectory();
        Console.Write("  RootFolder = "); SafeConsole.WriteLineColor(ConsoleColor.DarkGray, uctx.RootFolder);
        Console.Write("  ApplicationName = "); SafeConsole.WriteLineColor(ConsoleColor.DarkGray, uctx.ApplicationName);

        //SolutionRenamer.RenameSolution(uctx);

        //UpgradeContext.DefaultIgnoreDirectories = UpgradeContext.DefaultIgnoreDirectories.Where(a => a != "Framework").ToArray();

        new CodeUpgradeRunner(autoDiscover: true).Run(uctx);
    }
}
