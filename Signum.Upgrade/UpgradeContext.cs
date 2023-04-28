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

    public void DeleteFile(string fileName, WarningLevel showWarning = WarningLevel.Error)
    {
        fileName = this.AbsolutePathSouthwind(fileName);
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
        fileName = this.AbsolutePathSouthwind(fileName);
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
            codeFile.SaveIfNecessary();
        }
    }


    public void ForeachCodeFile(string searchPattern, Action<CodeFile> action, WarningLevel showWarnings = WarningLevel.None, string[]? ignoreDirectories = null) =>
        ForeachCodeFile(searchPattern, new[] { RootFolder }, action, showWarnings, ignoreDirectories);

    public void ForeachCodeFile(string searchPattern, string directory, Action<CodeFile> action, WarningLevel showWarnings = WarningLevel.None, string[]? ignoreDirectories = null) =>
        ForeachCodeFile(searchPattern, new[] { directory }, action, showWarnings, ignoreDirectories);

    public void ForeachCodeFile(string searchPattern, string[] directories, Action<CodeFile> action, WarningLevel showWarnings = WarningLevel.None, string[]? ignoreDirectories = null)
    {
        var searchPatterns = searchPattern.SplitNoEmpty(',').Select(a => a.Trim()).ToArray();

        foreach (var dir in directories)
        {
            var codeFiles = GetCodeFiles(dir, searchPatterns, ignoreDirectories);
            foreach (var codeFile in codeFiles)
            {
                codeFile.WarningLevel = showWarnings;
                action(codeFile);
                codeFile.SaveIfNecessary();
            }
        }
    }

    public List<CodeFile> GetCodeFiles(string directory, string[] searchPatterns, string[]? ignoreDirectories)
    {
        ignoreDirectories ??= DefaultIgnoreDirectories;

        var result = new List<CodeFile>();

        FillSourceCodeFiles(result, this.AbsolutePathSouthwind(directory), searchPatterns, ignoreDirectories);


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

    public void Solution_RemoveProject(string name, WarningLevel showWarning = WarningLevel.Error)
    {
        this.ChangeCodeFile(this.ApplicationName + ".sln", f =>
        {
            var prj = f.Content.Lines().Where(l => l.Contains("Project(") && l.Contains(name + ".csproj")).SingleOrDefault();

            if (prj == null)
            {
                SafeConsole.WriteLineColor(showWarning == WarningLevel.Error ? ConsoleColor.Red : ConsoleColor.Yellow,
                   showWarning.ToString().ToUpper() + " no reference to '" + name + "' found in " + f.FilePath);

                return;
            }

            var regex = new Regex(@"(?<id>[\w]{8}-[\w]{4}-[\w]{4}-[\w]{4}-[\w]{12})");

            var projectId = regex.Match(prj.After(name)).Groups["id"].Value;

            f.ReplaceBetween(l => l.Contains("Project(") && l.Contains(name + ".csproj"), 0, l => l.Contains("EndProject"), 0, "");

            f.RemoveAllLines(l => l.Contains(projectId));

        });
    }

    public void Solution_AddProject(string projectFile, string? parentFolder, WarningLevel showWarning = WarningLevel.Error)
    {
        this.ChangeCodeFile(this.ApplicationName + ".sln", f =>
        {
            var idRegex = new Regex(@"(?<id>[\w]{8}-[\w]{4}-[\w]{4}-[\w]{4}-[\w]{12})");

            var upgradeProject = f.Content.Lines().Where(l => l.Contains("Project(") && l.Contains("Signum.Upgrade.csproj")).FirstEx();

            var solutionId = idRegex.Match(upgradeProject.Before("Signum.Upgrade.csproj")).Groups["id"].Value;

            var prjRegex = new Regex(@"[\\]?(?<project>[\.\w]*).csproj");

            var prjName = prjRegex.Match(projectFile).Groups["project"].Value;

            var projectId = Guid.NewGuid().ToString();

            f.InsertAfterLastLine(l => l.StartsWith("EndProject"), $$"""
                Project("{{{solutionId}}}") = "{{prjName}}", "{{projectFile}}", "{{{projectId}}}"
                EndProject
                """);
            var configs = f.GetLinesBetween(l => l.Contains("GlobalSection(SolutionConfigurationPlatforms) = preSolution"), 1,
                l => l.Contains("EndGlobalSection"), -1).Split("\n");

            f.InsertAfterFirstLine(l => l.Contains("GlobalSection(ProjectConfigurationPlatforms) = postSolution"),
                configs.Select(config => $$"""
                    {{{projectId}}}.{{config.Before("=").Trim()}}.ActiveCfg = {{config.After("=").Trim()}}
                    {{{projectId}}}.{{config.Before("=").Trim()}}.Build.0 = {{config.After("=").Trim()}}
                """).ToString("\n"));

            if(parentFolder != null)
            {
                var parent = f.Content.Lines().Where(l => l.Contains("Project(") && l.Contains(parentFolder)).FirstEx();

                var parentId = idRegex.Match(parent.After(parentFolder)).Groups["id"].Value;

                f.InsertAfterFirstLine(l => l.Contains("GlobalSection(NestedProjects) = preSolution"), $"\t{{{projectId}}} = {{{parentId}}}");
            }
        });
        
        SafeConsole.WriteLineColor(ConsoleColor.Yellow, $"Project {projectFile} added to solution.");
    }

    public void Solution_MoveFiles(string source, string destination, string searchPattern, string[]? ignoreDirectories = null, bool @override = false)
    {
        var files = GetCodeFiles(source, searchPattern.SplitNoEmpty(',').Select(a => a.Trim()).ToArray(), ignoreDirectories);

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

            File.Move(from, to, @override);

            SafeConsole.WriteLineColor(ConsoleColor.Yellow, $"Moved {f.FilePath} -> {newFilePath}");
        }
    }

    public string ReplaceSouthwind(string val)
    {
        return val.Replace("Southwind", this.ApplicationName);
    }
}
