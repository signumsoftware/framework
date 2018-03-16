using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.Dynamic
{
    public class CheckRoslyn
    {
        public static void AssertRoslynIsPresent(string basePath, bool throwException)
        {
            if (!File.Exists(Path.Combine(basePath, @"bin\roslyn\csc.exe")))
            {
                SafeConsole.WriteLineColor(ConsoleColor.Yellow, $@"Dynamic requires roslyn compiler to be in {Directory.GetCurrentDirectory()}\bin\roslyn\csc.exe");

                var toolsPath = GetToolsPath(basePath);

                if (toolsPath != null)
                {
                    Directory.CreateDirectory(Path.Combine(basePath, @"bin\roslyn\"));

                    foreach (var file in Directory.GetFiles(toolsPath))
                        File.Copy(file, Path.Combine(basePath, @"bin\roslyn\", Path.GetFileName(file)));
                }
                else
                {
                    var message = $@"Impossible to copy roslyn automatically from packages folder, you need to do it manually!";

                    if (throwException)
                        throw new InvalidOperationException(message);
                    else
                        SafeConsole.WriteLineColor(ConsoleColor.Red, message);
                }
            }
        }

        private static string GetToolsPath(string basePath)
        {
            string packagesDir = Path.Combine(basePath, @"..\..\..\packages");
            if (!Directory.Exists(packagesDir))
                return null;
            
            var dir = Directory.GetDirectories(packagesDir, "Microsoft.Net.Compilers.*").OrderByDescending().FirstOrDefault();

            if (dir == null)
                return null;

            var result = Path.Combine(dir, "tools");

            if (Directory.Exists(result))
                return result;

            return null;
        }
    }
}
