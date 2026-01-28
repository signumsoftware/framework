using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Signum.Utilities;

namespace Signum.CodeGeneration;

public class SolutionRenamer
{
    static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".cshtml", ".json", ".txt", ".md", ".ts", ".tsx", ".js", ".jsx", ".csproj", ".sln", ".json",
        ".xml", ".yml", ".yaml", ".config", ".props", ".targets", ".html", ".css", ".ps1", ".gitignore"
    };



    public static void RenameSolution(Upgrade.UpgradeContext uctx)
    {
        Console.WriteLine("<< Rename Solution >>");

        if(SafeConsole.Ask("Are you running outside of Visual Studio and VS is closed?"))

        Console.Write($"Enter new solution name (current: {uctx.ApplicationName}): ");
        var newName = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(newName) || newName == uctx.ApplicationName)
        {
            Console.WriteLine("Invalid or unchanged name. Aborting.");
            return;
        }

        Console.Write($"Are you sure you want to rename '{uctx.ApplicationName}' to '{newName}'? (y/n): ");
        var confirm = Console.ReadLine();
        if (!string.Equals(confirm, "y", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Aborted by user.");
            return;
        }

        RenameRecursive(uctx.RootFolder, uctx.ApplicationName, newName, isRoot:true);

        Console.WriteLine("Renaming completed.");
    }

    static void RenameRecursive(string currentPath, string oldName, string newName, bool isRoot = false)
    {
        var dirName = Path.GetFileName(currentPath);
        if (dirName is "bin" or "obj" or ".git" or ".vs" or "ts_out" or "node_modules")
            return;

        // If this directory is a git submodule (not root), skip it
        if (!isRoot && File.Exists(Path.Combine(currentPath, ".gitignore")))
            return;

        // Process subdirectories first (bottom-up)
        foreach (var dir in Directory.GetDirectories(currentPath))
        {
            RenameRecursive(dir, oldName, newName, isRoot:false);
        }

        // Rename files in this directory and replace content
        foreach (var file in Directory.GetFiles(currentPath))
        {
            var ext = Path.GetExtension(file);
            var fileName = Path.GetFileName(file);
            if (TextExtensions.Contains(ext) || string.Equals(fileName, "Dockerfile", StringComparison.OrdinalIgnoreCase))
            {
                var content = File.ReadAllText(file);
                var newContent = content.Replace(oldName, newName).Replace(oldName.ToLower(), newName.ToLower());
                if (newContent != content)
                {
                    try
                    {
                        File.WriteAllText(file, newContent);
                    SafeConsole.WriteLineColor(ConsoleColor.Green, $"{file} File Updated");
                    }
                    catch (Exception e)
                    {
                        SafeConsole.WriteLineColor(ConsoleColor.Red, $"{currentPath} File Updating Error: " + e.Message);
                    }
                }
            }

            // Only rename the file name, not the whole path
            if (fileName.Contains(oldName))
            {
                var newFileName = fileName.Replace(oldName, newName).Replace(oldName.ToLower(), newName.ToLower());
                var newFilePath = Path.Combine(Path.GetDirectoryName(file)!, newFileName);
                try
                {
                    File.Move(file, newFilePath);
                    SafeConsole.WriteLineColor(ConsoleColor.Green, $"{file} File Renamed");
                }
                catch (Exception e)
                {
                    SafeConsole.WriteLineColor(ConsoleColor.Red, $"{currentPath} File Renaming Error: " + e.Message);
                }
            }
        }

        // Rename this directory if needed (never rename root)
        if (!isRoot && dirName != null && dirName.Contains(oldName))
        {
            var parent = Path.GetDirectoryName(currentPath);
            var newDirName = dirName.Replace(oldName, newName).Replace(oldName.ToLower(), newName.ToLower());
            var newDirPath = Path.Combine(parent!, newDirName);
            if (newDirPath != currentPath)
            {
                try
                {
                    Directory.Move(currentPath, newDirPath);

                    SafeConsole.WriteLineColor(ConsoleColor.Green, $"{currentPath} Directory Moved");
                }
                catch (Exception e)
                {
                    SafeConsole.WriteLineColor(ConsoleColor.Red, $"{currentPath} Directory Moving Error: " + e.Message);
                }
            }
        }
    }
}
