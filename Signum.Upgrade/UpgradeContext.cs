using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

namespace Signum.Upgrade
{
    public class UpgradeContext
    {
        public string RootFolder { get; set; }
        public string ApplicationName { get; set; }

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

        public string EntitiesDirectory => Path.Combine(RootFolder, ApplicationName + ".Entities");
        public string LogicDirectory => Path.Combine(RootFolder, ApplicationName + ".Logic");
        public string TerminalDirectory => Path.Combine(RootFolder, ApplicationName + ".Terminal");
        public string ReactDirectory => Path.Combine(RootFolder, ApplicationName + ".React");

        public bool HasWarnings { get; internal set; }

        public static string[] DefaultIgnoreDirectories = new[] { "bin", "obj", "CodeGen", "node_modules", "ts_out", "wwwroot", "Framework", "Extensions" };

        public void ChangeCodeFile(string fileName, Action<CodeFile> action, bool showWarnings = true)
        {
            fileName = fileName.Replace("Southwind", ApplicationName);
            if (!File.Exists(Path.Combine(this.RootFolder, fileName)))
            {
                HasWarnings = true;
                SafeConsole.WriteLineColor(ConsoleColor.Red, "WARNING file " + fileName + " not found");
            }
            else
            {
                var codeFile = new CodeFile(fileName, this) { ShowWarnings = showWarnings };
                action(codeFile);
                codeFile.SafeIfNecessary();
            }
        }


        public void ForeachCodeFile(string searchPattern, Action<CodeFile> action, bool showWarnings = false)
        {
            var codeFiles = GetCodeFiles(RootFolder, searchPattern.SplitNoEmpty(',').Select(a => a.Trim()).ToArray(), DefaultIgnoreDirectories);
            foreach (var codeFile in codeFiles)
            {
                codeFile.ShowWarnings = showWarnings;
                action(codeFile);
                codeFile.SafeIfNecessary();
            }
        }

        public void ForeachCodeFile(string searchPattern, string directory, Action<CodeFile> action, bool showWarnings = false)
        {
            var codeFiles = GetCodeFiles(directory, searchPattern.SplitNoEmpty(',').Select(a => a.Trim()).ToArray(), DefaultIgnoreDirectories);
            foreach (var codeFile in codeFiles)
            {
                codeFile.ShowWarnings = showWarnings;
                action(codeFile);
                codeFile.SafeIfNecessary();
            }
        }



    

        public void ForeachCodeFile(string searchPattern, string[] directories, Action<CodeFile> action, bool showWarnings = false)
        {

            foreach (var dir in directories)
            {
                var codeFiles = GetCodeFiles(dir, searchPattern.SplitNoEmpty(',').Select(a=>a.Trim()).ToArray(), DefaultIgnoreDirectories);
                foreach (var codeFile in codeFiles)
                {
                    codeFile.ShowWarnings = showWarnings;
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
}
