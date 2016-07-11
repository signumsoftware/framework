using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Signum.TSGenerator
{
    public class SignumTSGenerator : Task
    {
        [Required]
        public string References { get; set; }

        public override bool Execute()
        {
            AppDomain domain = AppDomain.CreateDomain("reflectionRomain");
            try
            {
                dynamic obj = domain.CreateInstanceFromAndUnwrap(this.GetType().Assembly.Location, "Signum.TSGenerator.ProxyGenerator");

                foreach (var file in Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.t4s", SearchOption.AllDirectories))
                {
                    try
                    {
                        Log.LogMessage($"Reading {file}");

                        string result = obj.Process(file, References, this.BuildEngine.ProjectFileOfTaskNode);

                        var targetFile = Path.ChangeExtension(file, ".ts");
                        Log.LogMessage($"Writing {targetFile}");
                        File.WriteAllText(targetFile, result);
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
                                      select new AssemblyReference
                                      {
                                          AssemblyName = Path.GetFileNameWithoutExtension(r),
                                          AssemblyFullPath = r,
                                          ReactDirectory = FindReactDirectory(r)
                                      }).ToDictionary(a => a.AssemblyName)
            };

            return EntityDeclarationGenerator.Process(options);
        }

        private static string ToWindows(string nodeRelative)
        {
            var relative = nodeRelative;
            if (nodeRelative.StartsWith("./"))
                nodeRelative = nodeRelative.Substring(2);

            return nodeRelative.Replace("/", @"\");
        }

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
