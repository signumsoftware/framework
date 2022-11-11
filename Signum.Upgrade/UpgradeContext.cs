using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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

        return lists.SingleEx(a => Directory.Exists(Path.Combine(rootFolder, a + ".Entities")));
    }

    public CodeFile? TryGetCodeFile(string fileName)
    {
        fileName = this.AbsolutePath(fileName.Replace("Southwind", ApplicationName));
        if (!File.Exists(fileName))
            return null;

        return new CodeFile(fileName, this);
    }

    public void DeleteFile(string fileName, WarningLevel showWarning = WarningLevel.Error)
    {
        fileName = this.AbsolutePath(fileName.Replace("Southwind", ApplicationName));
        if (!File.Exists(fileName))
        {
            if (showWarning != WarningLevel.None)
            {
                if (HasWarnings != WarningLevel.Error)
                    HasWarnings = showWarning;

                SafeConsole.WriteLineColor(showWarning == WarningLevel.Error ? ConsoleColor.Red : ConsoleColor.Yellow,
                     showWarning.ToString().ToUpper() + " file " + fileName + " not found");
            }

        }
        else
        {
            File.Delete(fileName);
        }
    }
           


    public void CreateCodeFile(string fileName, string content, WarningLevel showWarning = WarningLevel.Error)
    {
        fileName = this.AbsolutePath(fileName.Replace("Southwind", ApplicationName));
        if (File.Exists(fileName))
        {
            if (showWarning != WarningLevel.None)
            {
                if (HasWarnings != WarningLevel.Error)
                    HasWarnings = showWarning;

                SafeConsole.WriteLineColor(showWarning == WarningLevel.Error ? ConsoleColor.Red : ConsoleColor.Yellow,
                    showWarning.ToString().ToUpper() + " file " + fileName + " already exists");
            }
        }
        else
        {
            var dir = Path.GetDirectoryName(fileName)!;
            Directory.CreateDirectory(dir);
            File.WriteAllText(fileName, content, CodeFile.GetEncoding(fileName, null));
        }
    }

    public string AbsolutePath(string name) => Path.Combine(RootFolder, name);

    public string EntitiesDirectory => AbsolutePath(ApplicationName + ".Entities");
    public string LogicDirectory => AbsolutePath(ApplicationName + ".Logic");
    public string TerminalDirectory => AbsolutePath(ApplicationName + ".Terminal");
    public string ReactDirectory => AbsolutePath(ApplicationName + ".React");
    public string TestEnvironmentDirectory => AbsolutePath(ApplicationName + ".Test.Environment");
    public string TestLogicDirectory => AbsolutePath(ApplicationName + ".Test.Logic");
    public string TestReactDirectory => AbsolutePath(ApplicationName + ".Test.React");

    public WarningLevel HasWarnings { get; internal set; }

    public static string[] DefaultIgnoreDirectories = new[] { "bin", "obj", "CodeGen", "node_modules", "ts_out", "wwwroot", "Framework", ".git", ".vs", ".vscode" };

    public void ChangeCodeFile(string fileName, Action<CodeFile> action, WarningLevel showWarning = WarningLevel.Error)
    {
        fileName = fileName.Replace("Southwind", ApplicationName);
        if (!File.Exists(this.AbsolutePath(fileName)))
        {
            if (showWarning != WarningLevel.None)
            {
                if (HasWarnings != WarningLevel.Error)
                    HasWarnings = showWarning;

                SafeConsole.WriteLineColor(showWarning == WarningLevel.Error ? ConsoleColor.Red : ConsoleColor.Yellow,
                    showWarning.ToString().ToUpper() + " file " + fileName + " not found");
            }
        }
        else
        {
            var codeFile = new CodeFile(fileName, this) { WarningLevel = showWarning };
            action(codeFile);
            codeFile.SafeIfNecessary();
        }
    }


    public void ForeachCodeFile(string searchPattern, Action<CodeFile> action, WarningLevel showWarnings = WarningLevel.None)
    {
        var codeFiles = GetCodeFiles(RootFolder, searchPattern.SplitNoEmpty(',').Select(a => a.Trim()).ToArray(), DefaultIgnoreDirectories);
        foreach (var codeFile in codeFiles)
        {
            codeFile.WarningLevel = showWarnings;
            action(codeFile);
            codeFile.SafeIfNecessary();
        }
    }

    public void ForeachCodeFile(string searchPattern, string directory, Action<CodeFile> action, WarningLevel showWarnings = WarningLevel.None)
    {
        var codeFiles = GetCodeFiles(this.AbsolutePath(directory.Replace("Southwind", ApplicationName)), searchPattern.SplitNoEmpty(',').Select(a => a.Trim()).ToArray(), DefaultIgnoreDirectories);
        foreach (var codeFile in codeFiles)
        {
            codeFile.WarningLevel = showWarnings;
            action(codeFile);
            codeFile.SafeIfNecessary();
        }
    }

    public void ForeachCodeFile(string searchPattern, string[] directories, Action<CodeFile> action, WarningLevel showWarnings = WarningLevel.None)
    {
        foreach (var dir in directories)
        {
            var codeFiles = GetCodeFiles(this.AbsolutePath(dir.Replace("Southwind", ApplicationName)), searchPattern.SplitNoEmpty(',').Select(a => a.Trim()).ToArray(), DefaultIgnoreDirectories);
            foreach (var codeFile in codeFiles)
            {
                codeFile.WarningLevel = showWarnings;
                action(codeFile);
                codeFile.SafeIfNecessary();
            }
        }
    }

    public List<CodeFile> GetCodeFiles(string directory, string[] searchPatterns, string[] ignoreDirectories)
    {
        var result = new List<CodeFile>();

        FillSourceCodeFiles(result, directory, searchPatterns, ignoreDirectories);


        return result;
    }

    private void FillSourceCodeFiles(List<CodeFile> result, string directory, string[] searchPatterns, string[] ignoreDirectories)
    {
        foreach (var sp in searchPatterns)
        {
            result.AddRange(Directory.GetFiles(directory, sp, SearchOption.TopDirectoryOnly).Select(d => new CodeFile(Path.GetRelativePath(this.RootFolder, d), this)));
        }

        foreach (var dir in Directory.GetDirectories(directory).Where(d => !ignoreDirectories.Contains(Path.GetFileName(d))))
        {
            FillSourceCodeFiles(result, dir, searchPatterns, ignoreDirectories);
        }
    }
}
