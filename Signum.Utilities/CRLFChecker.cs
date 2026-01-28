using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Utilities;

public class CRLFChecker
{
    /// <summary>
    /// This method checks that your working directory is using LF and not CRLF
    /// Feel remove this call once you think the local repos of all the devs have been converted to LF
    /// </summary>
    public static void CheckGitCRLF()
    {
        var root = Path.Combine(Environment.CurrentDirectory, @"..\..\..\..");

        if (!Directory.Exists(Path.Combine(root, "Framework")))
            return; //No source code

        var stableFiles = new[]
        {
            @"Framework\Extensions\Signum.DiffLog\package.json",
            @"Framework\Extensions\Signum.Processes\package.json",
            @"Framework\Extensions\Signum.Toolbar\package.json"
        };

        if (stableFiles.Any(f => File.ReadAllText(Path.Combine(root, f)).Contains("\r\n")))
        {   
            Console.WriteLine("Looks like your working directory still has some CRLF files");

            if (SafeConsole.Ask("Do you want to fix it by executing some git commands?"))
            {
                if (SafeConsole.Ask("Your working directory will be overriden!! Have you commited all your code?"))
                {
                    ExecuteCommand("git", "rm --cached -r .", root);
                    ExecuteCommand("git", "reset --hard", root);
                    ExecuteCommand("git", "rm --cached -r .", Path.Combine(root, @"Framework"));
                    ExecuteCommand("git", "reset --hard", Path.Combine(root, @"Framework"));
                }
                else
                {
                    Console.WriteLine("Commit it first and come back later");
                }
            }
        }
    }

    static void ExecuteCommand(string cmd, string arguments, string directory)
    {   
        directory = Path.GetFullPath(directory);

        var processInfo = new ProcessStartInfo
        {
            FileName = cmd,
            Arguments = arguments,
            WorkingDirectory = directory, // Set the working directory for the process
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Console.WriteLine(@$"Executing ""{cmd} {arguments}"" in ""{directory}""");
        using (var process = Process.Start(processInfo)!)
        {
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            SafeConsole.WriteLineColor(ConsoleColor.Gray, "Output:\n" + output);
            if (!string.IsNullOrWhiteSpace(error.Trim()))
            {
                SafeConsole.WriteLineColor(ConsoleColor.Red, "Error:\n" + error);
            }

        }
}
}
