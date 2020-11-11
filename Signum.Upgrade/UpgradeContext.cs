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
            while (Directory.Exists(Path.Combine(directory, "Framework")))
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

        public static string[] DefaultIgnoreDirectories = new[] { "bin", "obj", "CodeGen", "node_modules", "ts_out", "wwwroot" };

        public void ForeachCodeFile(string searchPattern, Action<CodeFile> action)
        {
            var codeFiles = GetCodeFiles(RootFolder, searchPattern, DefaultIgnoreDirectories);
            foreach (var codeFile in codeFiles)
            {
                action(codeFile);
                codeFile.SafeIfNecessary();
            }
        }

        public void ForeachCodeFile(string searchPattern, string directory, Action<CodeFile> action)
        {
            var codeFiles = GetCodeFiles(directory, searchPattern, DefaultIgnoreDirectories);
            foreach (var codeFile in codeFiles)
            {
                action(codeFile);
                codeFile.SafeIfNecessary();
            }
        }

        public void ForeachCodeFile(string searchPattern, string[] directories, Action<CodeFile> action)
        {
            foreach (var dir in directories)
            {
                var codeFiles = GetCodeFiles(dir, searchPattern, DefaultIgnoreDirectories);
                foreach (var codeFile in codeFiles)
                {
                    action(codeFile);
                    codeFile.SafeIfNecessary();
                }
            }
        }

        public void ChangeCodeFile(string fileName, Action<CodeFile> action)
        {
            fileName = fileName.Replace("Southwind", ApplicationName);
            if (!Path.IsPathRooted(fileName))
                fileName = Path.Combine(RootFolder, fileName);

            var codeFile = new CodeFile(fileName, this);
            action(codeFile);
            codeFile.SafeIfNecessary();
        }


        public List<CodeFile> GetCodeFiles(string directory, string searchPattern, string[]? ignoreDirectories = null)
        {
            var result = new List<CodeFile>();

            FillSourceCodeFiles(result, directory, searchPattern, ignoreDirectories ?? DefaultIgnoreDirectories);

            return result;
        }

        private void FillSourceCodeFiles(List<CodeFile> result, string directory, string searchPattern, string[] ignoreDirectories)
        {
            result.AddRange(Directory.GetFiles(directory, searchPattern, SearchOption.TopDirectoryOnly).Select(d => new CodeFile(d, this)));

            foreach (var dir in Directory.GetDirectories(directory).Where(d => !ignoreDirectories.Contains(Path.GetFileName(d))))
            {
                FillSourceCodeFiles(result, dir, searchPattern, ignoreDirectories);
            }
        }
    }
}
