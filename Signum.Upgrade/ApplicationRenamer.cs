using LibGit2Sharp;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Upgrade;

internal class ApplicationRenamer
{
    public static void RenameApplication(UpgradeContext uctx)
    {
        Console.WriteLine($"This class will help you rename all the files names and file content from '{uctx.ApplicationName}' to a new name");

        while (CodeUpgradeRunner.IsDirtyExceptSubmodules(uctx.RootFolder))
        {
            Console.WriteLine();
            Console.WriteLine("There are changes in the git repo. Commit or reset the changes and press [Enter]");
            Console.ReadLine();
        }

        var newName = SafeConsole.AskString("New name?", s => Regex.IsMatch(s, @"[A-Z][a-zA-Z0-9]+") ? null : "New name should start by Upercase and then continue with numbers or letters");
        InternalRenameDirectoryTree(new DirectoryInfo(uctx.RootFolder), UpgradeContext.DefaultIgnoreDirectories, n =>
        {
            return n
            .Replace(uctx.ApplicationName.ToLower(), newName.ToLower())
            .Replace(uctx.ApplicationName.ToUpper(), newName.ToUpper())
            .Replace(uctx.ApplicationName, newName);
        });
        Commit(uctx, $"Rename file names from {uctx.ApplicationName} to {newName}");

        RenameContent(uctx, newName);
        Commit(uctx, $"Rename content from {uctx.ApplicationName} to {newName}");
    }

    private static void RenameContent(UpgradeContext uctx, string newName)
    {
        uctx.ForeachCodeFile("*.*", c =>
        {
            c.Replace(uctx.ApplicationName.ToLower(), newName.ToLower());
            c.Replace(uctx.ApplicationName.ToUpper(), newName.ToUpper());
            c.Replace(uctx.ApplicationName, newName);
        });
    }

    static void InternalRenameDirectoryTree(DirectoryInfo di, string[] ignoreDirectoryNames, Func<string, string> renamingRule)
    {
        foreach (var item in di.GetFileSystemInfos())
        {
            var subdir = item as DirectoryInfo;
            if (subdir != null)
            {
                if (!ignoreDirectoryNames.Contains(subdir.Name))
                {
                    InternalRenameDirectoryTree(subdir, ignoreDirectoryNames, renamingRule);

                    var currentName = subdir.Name;
                    var newName = renamingRule(currentName);
                    if (currentName != newName)
                    {
                        var newDirname = Path.Combine(subdir.Parent!.FullName, newName);
                        if (Directory.Exists(newDirname) && SafeConsole.Ask($"{newDirname} already exist. Delete?"))
                            Directory.Delete(newDirname, true);
                        subdir.MoveTo(newDirname);
                    }
                }
            }

            var file = item as FileInfo;
            if (file != null)
            {
                var currentName = Path.GetFileNameWithoutExtension(file.Name);
                var newName = renamingRule(currentName);
                if (currentName != newName)
                {
                    var newFilename = Path.Combine(file.DirectoryName!, newName + file.Extension);
                    file.MoveTo(newFilename);
                }
            }
        }

    }

    private static void Commit(UpgradeContext uctx, string message)
    {
        using (Repository rep = new Repository(uctx.RootFolder))
        {
            if (rep.RetrieveStatus().IsDirty)
            {
                Commands.Stage(rep, "*");
                var sign = rep.Config.BuildSignature(DateTimeOffset.Now);
                rep.Commit(message, sign, sign);
                SafeConsole.WriteLineColor(ConsoleColor.White, "A commit with text message '{0}' has been created".FormatWith(message));
            }
            else
            {
                Console.WriteLine("Nothing to commit");
            }
        }
    }
}
