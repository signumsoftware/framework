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
            var refs = referenceList.Split(';').ToDictionary(a => Path.GetFileName(a));

            var fileContent = File.ReadAllText(templateFile);

            Dictionary<string, string> parameters = new Dictionary<string, string>();

            fileContent = Regex.Replace(fileContent, @"//(?<key>\w*(\.\w*)?):\s*(?<value>.*?)\s*$\n", m =>
            {
                var key = m.Groups["key"].Value;

                if (parameters.ContainsKey(key))
                    throw new LoggerException($"Meta-Comment '{key}' is repeated") { Data = { ["LineNumber"] = Utils.SetLineNumbers(m, fileContent) } };

                parameters.Add(key, m.Groups["value"].Value);
                return "";
            }, RegexOptions.Multiline);

   
            var references = (from Match m in Regex.Matches(fileContent, @"import \* as (?<variable>\w+) from '(?<path>.+?)'")
                              let var = m.Groups["variable"].Value
                              select new Reference
                              {
                                  Match = m,
                                  VariableName = var,
                                  AssemblyFullPath = refs.GetReferencedAssembly(parameters.ConsumeParameter(var + ".Assembly"), projectFile),
                                  BaseNamespace = parameters.TryConsumeParameter(var + ".BaseNamespace") ?? Path.GetFileName(m.Groups["path"].Value),
                              }).ToList();

            AssertNoDuplicate(references, r => r.VariableName, "VariableName", fileContent);
            AssertNoDuplicate(references, r => r.AssemblyFullPath + "/" + r.BaseNamespace, "AssemlyFullPath and BaseNamespace", fileContent);

            var options = new Options(refs.GetReferencedAssembly(parameters.ConsumeParameter("Assembly"), projectFile))
            {
                BaseNamespace = parameters.TryConsumeParameter("BaseNamespace") ?? Path.GetFileNameWithoutExtension(templateFile),
                References = references,
            };

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"//////////////////////////////////");
            sb.AppendLine(@"//Auto-generated. Do NOT modify!//");
            sb.AppendLine(@"//////////////////////////////////");

            sb.AppendLine(fileContent);

            EntityDeclarationGenerator.Process(sb, options);

            return sb.ToString();
        }

        private void AssertNoDuplicate(List<Reference> references, Func<Reference, string> selector, string typeOfthing, string fileContent)
        {
            HashSet<string> already = new HashSet<string>();
            foreach (var r in references)
            {
                var key = selector(r);

                if(already.Contains(key))
                    throw new LoggerException($"Duplicated {typeOfthing} '{key}'") { Data = { ["LineNumber"] = Utils.SetLineNumbers(r.Match, fileContent) } };

                already.Add(key);
            }
        }
    }

    public static class Utils
    {
        public static string GetReferencedAssembly(this Dictionary<string, string> dictionary, string assemblyName, string projectName)
        {
            string value;
            if (!dictionary.TryGetValue(assemblyName, out value))
                throw new InvalidOperationException($"No reference to '{assemblyName}' found in {projectName}.");

            return value;
        }

        public static V ConsumeParameter<K, V>(this Dictionary<K, V> dictionary, K key)
        {
            V value;
            if (!dictionary.TryGetValue(key, out value))
                throw new InvalidOperationException($"No parameter '{key}' found. Write something like //{key}: yourValue");

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
