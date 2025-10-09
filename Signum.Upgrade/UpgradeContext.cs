using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;

namespace Signum.Upgrade;

public class UpgradeContext
{
    public string RootFolder { get; set; }
    public string ApplicationName { get; set; }

    static UpgradeContext()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public UpgradeContext(string rootFolder, string applicationName)
    {
        RootFolder = rootFolder;
        ApplicationName = applicationName;
    }

    public static UpgradeContext CreateFromCurrentDirectory()
    {

        var rootFolder = GetRootFolder();
        var applicationName = GetApplicationName(rootFolder);

        return new UpgradeContext(rootFolder, applicationName);
    }

    static string GetRootFolder()
    {
        var directory = Directory.GetCurrentDirectory()!;
        while (!Directory.Exists(Path.Combine(directory, "Framework")))
        {
            directory = Path.GetDirectoryName(directory);
            if (directory == null)
                throw new InvalidOperationException("Unable to detect Root Folder");
        }

        return directory;
    }

    static string GetApplicationName(string rootFolder)
    {
        var lists = Directory.GetFiles(rootFolder, "*.sln").Select(a => Path.GetFileNameWithoutExtension(a)).ToList();

        return lists.SingleEx(a => Directory.Exists(Path.Combine(rootFolder, a)) || Directory.Exists(Path.Combine(rootFolder, a + ".Entities")));
    }

    public CodeFile? TryGetCodeFile(string fileName)
    {
        fileName = this.AbsolutePathSouthwind(fileName);
        if (!File.Exists(fileName))
            return null;

        return new CodeFile(fileName, this);
    }

    public void DeleteFile(string fileName, WarningLevel fileWarning = WarningLevel.Error)
    {
        var fullFileName = this.AbsolutePathSouthwind(fileName);
        if (!File.Exists(fullFileName))
        {
            if (fileWarning != WarningLevel.None)
            {
                if (HasWarnings != WarningLevel.Error)
                    HasWarnings = fileWarning;

                SafeConsole.WriteLineColor(fileWarning == WarningLevel.Error ? ConsoleColor.Red : ConsoleColor.Yellow,
                     fileWarning.ToString().ToUpper() + " file " + fileName + " not found");
            }

        }
        else
        {
            File.Delete(fullFileName);
            SafeConsole.WriteLineColor(ConsoleColor.DarkGray, "Deleted " + fileName);
        }
    }



    public void CreateCodeFile(string fileName, string content, WarningLevel fileWarning = WarningLevel.Error)
    {
        fileName = this.AbsolutePathSouthwind(fileName);
        if (File.Exists(fileName))
        {
            if (fileWarning != WarningLevel.None)
            {
                if (HasWarnings != WarningLevel.Error)
                    HasWarnings = fileWarning;

                SafeConsole.WriteLineColor(fileWarning == WarningLevel.Error ? ConsoleColor.Red : ConsoleColor.Yellow,
                    fileWarning.ToString().ToUpper() + " file " + fileName + " already exists");
            }
        }
        else
        {
            var dir = Path.GetDirectoryName(fileName)!;
            Directory.CreateDirectory(dir);
            File.WriteAllText(fileName, content, CodeFile.GetEncoding(fileName, null));
        }
    }

    public string AbsolutePathSouthwind(string name) => Path.Combine(RootFolder, name.Replace("Southwind", this.ApplicationName));
    public string AbsolutePath(string name) => Path.Combine(RootFolder, name);

    public string EntitiesDirectory => AbsolutePath(ApplicationName + ".Entities");
    public string LogicDirectory => AbsolutePath(ApplicationName + ".Logic");
    public string TerminalDirectory => AbsolutePath(ApplicationName + ".Terminal");
    public string ReactDirectory => AbsolutePath(ApplicationName + ".React");
    public string TestEnvironmentDirectory => AbsolutePath(ApplicationName + ".Test.Environment");
    public string TestLogicDirectory => AbsolutePath(ApplicationName + ".Test.Logic");
    public string TestReactDirectory => AbsolutePath(ApplicationName + ".Test.React");

    public WarningLevel HasWarnings { get; internal set; }

    public static string[] DefaultIgnoreDirectories = new[] { "bin", "obj", "CodeGen", "node_modules", "ts_out", "dist", "Framework", ".git", ".vs", ".vscode" };

    public void ChangeCodeFile(string fileName, Action<CodeFile> action, WarningLevel showWarnings = WarningLevel.Error)
    {
        fileName = fileName.Replace("Southwind", ApplicationName);
        if (!File.Exists(this.AbsolutePath(fileName)))
        {
            if (showWarnings != WarningLevel.None)
            {
                if (HasWarnings != WarningLevel.Error)
                    HasWarnings = showWarnings;

                SafeConsole.WriteLineColor(showWarnings == WarningLevel.Error ? ConsoleColor.Red : ConsoleColor.Yellow,
                    showWarnings.ToString().ToUpper() + " file " + fileName + " not found");
            }
        }
        else
        {
            var codeFile = new CodeFile(fileName, this) { WarningLevel = showWarnings };
            action(codeFile);
            codeFile.SaveIfNecessary();
        }
    }


    public void ForeachCodeFile(string searchPattern, Action<CodeFile> action, WarningLevel codeWarning = WarningLevel.None, WarningLevel directoryWarning = WarningLevel.Error, string[]? ignoreDirectories = null) =>
        ForeachCodeFile(searchPattern, new[] { RootFolder }, action, codeWarning, directoryWarning, ignoreDirectories);

    public void ForeachCodeFile(string searchPattern, string directory, Action<CodeFile> action, WarningLevel codeWarning = WarningLevel.None, WarningLevel directoryWarning = WarningLevel.Error, string[]? ignoreDirectories = null) =>
        ForeachCodeFile(searchPattern, new[] { directory }, action, codeWarning, directoryWarning, ignoreDirectories);

    public void ForeachCodeFile(string searchPattern, string[] directories, Action<CodeFile> action, WarningLevel codeWarning = WarningLevel.None, WarningLevel directoryWarning = WarningLevel.Error, string[]? ignoreDirectories = null)
    {
        var searchPatterns = searchPattern.SplitNoEmpty(',').Select(a => a.Trim()).ToArray();

        foreach (var dir in directories)
        {
            var codeFiles = GetCodeFiles(dir, searchPatterns, ignoreDirectories, directoryWarning);
            foreach (var codeFile in codeFiles)
            {
                codeFile.WarningLevel = codeWarning;
                action(codeFile);
                codeFile.SaveIfNecessary();
            }
        }
    }

    public List<CodeFile> GetCodeFiles(string directory, string[] searchPatterns, string[]? ignoreDirectories, WarningLevel showWarnings)
    {
        ignoreDirectories ??= DefaultIgnoreDirectories;

        var result = new List<CodeFile>();

        var absoluteDirectory = this.AbsolutePathSouthwind(directory);

        if (!Directory.Exists(absoluteDirectory))
        {
            if (showWarnings != WarningLevel.None)
            {
                if (HasWarnings != WarningLevel.Error)
                    HasWarnings = showWarnings;

                SafeConsole.WriteLineColor(showWarnings == WarningLevel.Error ? ConsoleColor.Red : ConsoleColor.Yellow,
                    showWarnings.ToString().ToUpper() + " directory " + absoluteDirectory + " not found");
            }
        }
        else
        {
            FillSourceCodeFiles(result, absoluteDirectory, searchPatterns, ignoreDirectories);
        }

        return result;
    }

    private void FillSourceCodeFiles(List<CodeFile> result, string absoluteDirectory, string[] searchPatterns, string[] ignoreDirectories)
    {
        foreach (var sp in searchPatterns)
        {
            result.AddRange(Directory.GetFiles(absoluteDirectory, sp, SearchOption.TopDirectoryOnly).Select(d => new CodeFile(Path.GetRelativePath(this.RootFolder, d), this)));
        }

        foreach (var dir in Directory.GetDirectories(absoluteDirectory).Where(d => !ignoreDirectories.Contains(Path.GetFileName(d))))
        {
            FillSourceCodeFiles(result, dir, searchPatterns, ignoreDirectories);
        }
    }

   

    public void MoveFiles(string source, string destination, string searchPattern, string[]? ignoreDirectories = null, bool @override = false, WarningLevel directoryWarning = WarningLevel.Error)
    {
        var files = GetCodeFiles(source, searchPattern.SplitNoEmpty(',').Select(a => a.Trim()).ToArray(), ignoreDirectories, directoryWarning);

        string Normalize(string dirName)
        {
            return dirName.Replace("Southwind", ApplicationName).Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        }

        foreach (var f in files)
        {
            var newFilePath = Normalize(destination) + f.FilePath.After(Normalize(source));
            
            var from = this.AbsolutePath(f.FilePath);

            var to = this.AbsolutePath(newFilePath)!;

            var newDir = Path.GetDirectoryName(to)!;

            if (!Directory.Exists(newDir))
                Directory.CreateDirectory(newDir);

            if(!@override && !File.Exists(to))
            {
                File.Move(from, to, @override);
                SafeConsole.WriteLineColor(ConsoleColor.Yellow, $"Moved {f.FilePath} -> {newFilePath}");
            }
        }
    }

    public string ReplaceSouthwind(string val)
    {
        return val.Replace("Southwind", this.ApplicationName);
    }

    public void MoveFile(string from, string to, bool createDirectory = false, WarningLevel showWarning = WarningLevel.Error)
    {
        var fromAbs = AbsolutePathSouthwind(from);
        var toAbs = AbsolutePathSouthwind(to);
        if (!File.Exists(fromAbs))
        {
            SafeConsole.WriteLineColor(showWarning == WarningLevel.Error ? ConsoleColor.Red : ConsoleColor.Yellow,
                  $"Unanble to move {from} -> {to}: File not found");

            return;
        }

        if (createDirectory)
            Directory.CreateDirectory(Path.GetDirectoryName(toAbs)!);

        File.Move(fromAbs, toAbs);

        SafeConsole.WriteLineColor(ConsoleColor.Yellow, $"Moved {from} -> {to}");
    }

    public void DeleteDirectory(string directory, WarningLevel showWarning = WarningLevel.Error)
    {
        try
        {
            Directory.Delete(AbsolutePathSouthwind(directory), true);

            SafeConsole.WriteLineColor(ConsoleColor.Yellow, $"Directory deleted {directory}");
        }
        catch(Exception e)
        {
            SafeConsole.WriteLineColor(showWarning == WarningLevel.Error ? ConsoleColor.Red : ConsoleColor.Yellow,
                  $"Uanble to delete {directory}:" + e.Message);
        }
    }
}
