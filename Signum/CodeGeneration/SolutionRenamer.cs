using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Signum.CodeGeneration;

public class SolutionRenamer
{
    private readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".json", ".txt", ".md", ".ts", ".tsx", ".js", ".jsx", ".csproj", ".sln", ".json",
        ".xml", ".yml", ".yaml", ".config", ".props", ".targets", ".html", ".css", ".ps1", ".gitignore"
    };

    public void RenameSolution()
    {
        CodeGenerator.GetSolutionInfo(out var solutionFolder, out var solutionName);

        Console.Write($"Enter new solution name (current: {solutionName}): ");
        var newName = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(newName) || newName == solutionName)
        {
            Console.WriteLine("Invalid or unchanged name. Aborting.");
            return;
        }

        Console.Write($"Are you sure you want to rename '{solutionName}' to '{newName}'? (y/n): ");
        var confirm = Console.ReadLine();
        if (!string.Equals(confirm, "y", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Aborted by user.");
            return;
        }

        RenameRecursive(solutionFolder, solutionName, newName, isRoot:true);

        Console.WriteLine("Renaming completed.");
    }

    protected virtual void RenameRecursive(string currentPath, string oldName, string newName, bool isRoot = false)
    {
        var dirName = Path.GetFileName(currentPath);
        if (dirName is "bin" or "obj" or ".git" or "ts_out" or "node_modules")
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
                var newContent = content.Replace(oldName, newName);
                if (newContent != content)
                {
                    SafeConsole.WriteLineColor(ConsoleColor.Yellow, $"{file} File Updated");
                    File.WriteAllText(file, newContent);
                }
            }

            // Only rename the file name, not the whole path
            if (fileName.Contains(oldName))
            {
                var newFileName = fileName.Replace(oldName, newName);
                var newFilePath = Path.Combine(Path.GetDirectoryName(file)!, newFileName);
                SafeConsole.WriteLineColor(ConsoleColor.Green, $"{file} File Renamed");
                File.Move(file, newFilePath);
            }
        }

        // Rename this directory if needed (never rename root)
        if (!isRoot && dirName != null && dirName.Contains(oldName))
        {
            var parent = Path.GetDirectoryName(currentPath);
            var newDirName = dirName.Replace(oldName, newName);
            var newDirPath = Path.Combine(parent!, newDirName);
            if (newDirPath != currentPath)
            {
                SafeConsole.WriteLineColor(ConsoleColor.Green, $"{currentPath} Directory Moved");
                Directory.Move(currentPath, newDirPath);
            }
        }
    }
}
