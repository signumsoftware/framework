using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

namespace Signum.TSGenerator
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            string projectFile = args[0];
            string[] references = File.ReadAllLines(args[1]);
            string[] content = File.ReadAllLines(args[2]);

            var log = Console.Out;

            var obj = new ProxyGenerator();

            log.WriteLine("Starting SignumTSGenerator");

            var currentDir = Directory.GetCurrentDirectory();
            var files = content
                .Where(file => Path.GetExtension(file) == ".t4s")
                .Select(file => Path.Combine(currentDir, file))
                .ToList();

            bool hasErrors = false;
            foreach (var file in files)
            {
                try
                {
                    //log.WriteLine($"Reading {file}");

                    string result = obj.Process(file, references, projectFile);

                    var targetFile = Path.ChangeExtension(file, ".ts");
                    if (File.Exists(targetFile) && File.ReadAllText(targetFile) == result)
                    {
                        log.WriteLine($"Skipping {targetFile} (Up to date)");
                    }
                    else
                    {
                        log.WriteLine($"Writing {targetFile}");
                        File.WriteAllText(targetFile, result);
                    }
                }
                catch (Exception ex)
                {
                    hasErrors = true;
                    log.WriteLine($"Error in {file}");
                    log.WriteLine(ex.Message);
                }
            }

            log.WriteLine("Finish SignumTSGenerator");

            return hasErrors ? -1 : 0;
        }
    }

    public class ProxyGenerator
    {
        public string Process(string templateFile, string[] referenceList, string projectFile)
        {
            var options = new Options
            {
                TemplateFileName = templateFile,
                CurrentNamespace = Path.GetFileNameWithoutExtension(templateFile),
                CurrentAssembly = Path.GetFileNameWithoutExtension(projectFile).Replace(".React", ".Entities"),
                AssemblyReferences = (from r in referenceList
                                      where r.Contains(".Entities")
                                      let reactDirectory = ReactDirectoryCache.GetOrAdd(r, FindReactDirectory)
                                      select new AssemblyReference
                                      {
                                          AssemblyName = Path.GetFileNameWithoutExtension(r),
                                          AssemblyFullPath = r,
                                          ReactDirectory = reactDirectory,
                                          AllTypescriptFiles = AllFilesCache.GetOrAdd(reactDirectory, GetAllTypescriptFiles),
                                      }).ToDictionary(a => a.AssemblyName),
                AllReferences = referenceList.ToDictionary(a => Path.GetFileNameWithoutExtension(a)),
            };

            PreloadingAssemblyResolver resolver = new PreloadingAssemblyResolver(referenceList);

            return EntityDeclarationGenerator.Process(options, resolver);
        }

        ConcurrentDictionary<string, List<string>> AllFilesCache = new ConcurrentDictionary<string, List<string>>();
        public static List<string> GetAllTypescriptFiles(string reactDirectory)
        {
            return new DirectoryInfo(reactDirectory).EnumerateFiles("*.ts", SearchOption.AllDirectories)
                .Concat(new DirectoryInfo(reactDirectory).EnumerateFiles("*.t4s", SearchOption.AllDirectories))
                .Select(a => a.FullName)
                .Where(fn => !fn.Contains(@"\obj\") && !fn.Contains(@"\bin\")) //Makes problem when deploying
                .ToList();
        }

        ConcurrentDictionary<string, string> ReactDirectoryCache = new ConcurrentDictionary<string, string>();

        private string FindReactDirectory(string absoluteFilePath)
        {
            var prefix = absoluteFilePath;
            while (prefix != null)
            {
                var name = Path.GetFileName(prefix);

                if (name.Contains(".Entities"))
                {
                    name = name.Replace(".Entities", ".React");
                    var dir = Path.Combine(Path.GetDirectoryName(prefix), name);
                    if (Directory.Exists(dir))
                        return dir;
                }

                prefix = Path.GetDirectoryName(prefix);
            }

            throw new InvalidOperationException("Impossible to determine the react directory for '" + absoluteFilePath + "'");
        }
    }

    public static class Utils
    {
        public static string GetReferencedAssembly(this Dictionary<string, string> refs, string assemblyName, string projectName)
        {
            string value;
            if (!refs.TryGetValue(assemblyName, out value))
                throw new InvalidOperationException($"No reference to '{assemblyName}' found in {projectName}.");

            return value;
        }

        public static V TryConsumeParameter<K, V>(this Dictionary<K, V> dictionary, K key) where V : class
        {
            V value;
            if (!dictionary.TryGetValue(key, out value))
                return null;


            return value;
        }

        public static int SetLineNumbers(Match m, string file)
        {
            var subStr = file.Substring(0, m.Index);

            return subStr.Count(c => c == '\n') + 1;
        }
    }
}
