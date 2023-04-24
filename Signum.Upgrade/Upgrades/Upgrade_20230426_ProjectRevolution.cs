using LibGit2Sharp;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20230426_ProjectRevolution : CodeUpgradeBase
{
    public override string Description => "Project REVOLUTION";

    public override void Execute(UpgradeContext uctx)
    {
        SafeConsole.WriteLineColor(ConsoleColor.Magenta, "This upgrade will completely re-structure your application!!");
        Console.WriteLine("Some important considerations:");
        Console.WriteLine("* After running this upgrade, manual work is expected fixing namespaces, so it is recommended that you run it from the framework branch origin/revolution to avoid extra changes that could come in the future.");
        Console.WriteLine("* Read XXXXX before continuing.");

        Console.WriteLine();
        Console.WriteLine("Press any key when you have read it");

        Console.ReadLine();

        var entities = uctx.AbsolutePath("Southwind.Entities");
        var logic = uctx.AbsolutePath("Southwind.Logic");
        var react = uctx.AbsolutePath("Southwind.React");

        Console.WriteLine("The following projects are going to be combined together");
        Console.WriteLine("* " + entities);
        Console.WriteLine("* " + logic);
        Console.WriteLine("* " + react);

        var entitiesDirectories = Directory.GetDirectories(entities).Select(a => new { Module = Path.GetFileName(a), Source = "Entities" }).ToList();
        var logicDirectories = Directory.GetDirectories(logic).Select(a => new { Module = Path.GetFileName(a), Source = "Logic" }).ToList();
        var reactDirectories = Directory.GetDirectories(Path.Combine(react, "App")).Select(a => new { Module = Path.GetFileName(a), Source = @"React\App" }).ToList();

        Console.WriteLine($"With the current structure, this modules will be created for the new project '{uctx.AbsolutePath("Southwind")}'");

        var resultDirectories = entitiesDirectories.Concat(logicDirectories).Concat(reactDirectories)
            .GroupBy(a => a.Module, a => a.Source)
            .ToString(a => $" {a.Key} ({a.ToString(", ")})", "\n");

        Console.Write(resultDirectories);

        Console.WriteLine("To keep things organized in modules, it is recommended to have a similar folder structure before in each folder.");

        if(!SafeConsole.Ask("Are you happy with this structure?"))
        {
            Console.WriteLine("Organize your source code in a parallel folder structure, " +
                "don't bother about making it compile since the namespaes are going to change anyway (MyApp.Entities.Customers -> MyApp.Customers), " +
                "and execute the Upgrade again when you are ready.");

            throw new InvalidOperationException("Execute the Upgrade again when you are ready.");
        }

        


       
    }
}


//uctx.ForeachCodeFile("*.js", "Framework", a =>
//{
//    uctx.DeleteFile(a.FilePath);
//});

//uctx.ForeachCodeFile("*.js.map", "Framework", a =>
//{
//    uctx.DeleteFile(a.FilePath);
//});

//uctx.ForeachCodeFile("*.d.ts", "Framework", a =>
//{
//    uctx.DeleteFile(a.FilePath);
//});

//uctx.ForeachCodeFile("*.d.ts.map", "Framework", a =>
//{
//    uctx.DeleteFile(a.FilePath);
//});
