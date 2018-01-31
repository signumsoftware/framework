using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Collections.Concurrent;

namespace Signum.TSGenerator
{
    public class SignumTSGenerator : Task
    {
        [Required]
        public string References { get; set; }

        [Required]
        public string Content { get; set; }

        public override bool Execute()
        {
            AppDomain domain = AppDomain.CreateDomain("reflectionRomain");
            try
            {
                dynamic obj = domain.CreateInstanceFromAndUnwrap(this.GetType().Assembly.Location, "Signum.TSGenerator.ProxyGenerator");

                Log.LogMessage("Starting SigumTSGenerator");

                var currentDir = Directory.GetCurrentDirectory();
                var files = Content.Split(';')
                    .Where(file => Path.GetExtension(file) == ".t4s")
                    .Select(file => Path.Combine(currentDir, file))
                    .ToList();

                foreach (var file in files)
                {
                    try
                    {
                        Log.LogMessage($"Reading {file}");

                        string result = obj.Process(file, References, this.BuildEngine.ProjectFileOfTaskNode);

                        var targetFile = Path.ChangeExtension(file, ".ts");
                        if (File.Exists(targetFile) && File.ReadAllText(targetFile) == result)
                        {
                            Log.LogMessage($"Skipping {targetFile} (Up to date)");
                        }
                        else
                        {
                            Log.LogMessage($"Writing {targetFile}");
                            File.WriteAllText(targetFile, result);
                        }
                    }
                    catch (LoggerException ex)
                    {
                        Log.LogError(null, ex.ErrorCode, ex.HelpKeyword, file, (int)ex.Data["LineNumber"], 0, 0, 0, ex.Message);
                    }
                    catch (Exception ex)
                    {
                        Log.LogError(null, null, null, file, 0, 0, 0, 0, ex.Message);
                    }
                }


                Log.LogMessage("Finish SigumTSGenerator");
            }
            finally
            {
                AppDomain.Unload(domain);
            }

            return !Log.HasLoggedErrors;
        }
    }

    public class ProxyGenerator: MarshalByRefObject
    {
        public string Process(string templateFile, string referenceList, string projectFile)
        {
            var options = new Options
            {
                TemplateFileName = templateFile,
                CurrentNamespace = Path.GetFileNameWithoutExtension(templateFile),
                CurrentAssembly = Path.GetFileNameWithoutExtension(projectFile).Replace(".React", ".Entities"),
                AssemblyReferences = (from r in referenceList.Split(';')
                                      where r.Contains(".Entities")
                                      let reactDirectory = ReactDirectoryCache.GetOrAdd(r, FindReactDirectory)
                                      select new AssemblyReference
                                      {
                                          AssemblyName = Path.GetFileNameWithoutExtension(r),
                                          AssemblyFullPath = r,
                                          ReactDirectory = reactDirectory,
                                          AllTypescriptFiles = AllFilesCache.GetOrAdd(reactDirectory, GetAllTypescriptFiles),
                                      }).ToDictionary(a => a.AssemblyName)
            };

            return EntityDeclarationGenerator.Process(options);
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
