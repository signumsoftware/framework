using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Signum.TSGenerator
{
    public class SignumTSGenerator : Task
    {
        [Required]
        public string TemplateFile { get; set; }

        [Required]
        public string References { get; set; }

        public override bool Execute()
        {
            Log.LogMessage($"Reading {TemplateFile}");

            string result;
            AppDomain domain = AppDomain.CreateDomain("reflectionRomain");
            try
            {
                dynamic obj = domain.CreateInstanceFromAndUnwrap(this.GetType().Assembly.Location, "Signum.TSGenerator.ProxyGenerator");
                result = obj.Process(TemplateFile, References, this.BuildEngine.ProjectFileOfTaskNode);
            }
            finally
            {
                AppDomain.Unload(domain);
            }

            var targetFile = Path.ChangeExtension(TemplateFile, ".ts");
            Log.LogMessage($"Writing {targetFile}");
            File.WriteAllText(targetFile, result);

            return true;
        }
    }

    public class ProxyGenerator: MarshalByRefObject
    {
        public string Process(string templateFile, string referenceList, string projectFile)
        {
            var refs = referenceList.Split(';').ToDictionary(a => Path.GetFileName(a));

            var file = File.ReadAllText(templateFile);

            Dictionary<string, string> parameters = new Dictionary<string, string>();

            file = Regex.Replace(file, @"//(?<key>\w*(\.\w*)?):\s*(?<value>.*?)\s*$\n", m =>
            {
                parameters.Add(m.Groups["key"].Value, m.Groups["value"].Value);
                return "";
            }, RegexOptions.Multiline);

            var imports = Regex.Matches(file, @"import \* as (?<variable>\w+) from '.*?'").Cast<Match>().Select(a => a.Groups["variable"].Value).ToList();

            var references = imports
                .Select(var => new Reference
                {
                    VariableName = var,
                    AssemblyFullPath = refs.GetReferencedAssembly(parameters.ConsumeParameter(var + ".Assembly"), projectFile),
                    BaseNamespace = parameters.ConsumeParameter(var + ".BaseNamespace"),
                }).ToList();

            var options = new Options(refs.GetReferencedAssembly(parameters.ConsumeParameter("Assembly"), projectFile))
            {
                BaseNamespace = parameters.ConsumeParameter("BaseNamespace"),
                References = references,
            };

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"//////////////////////////////////");
            sb.AppendLine(@"//Auto-generated. Do NOT modify!//");
            sb.AppendLine(@"//////////////////////////////////");

            sb.AppendLine(file);

            EntityDeclarationGenerator.Process(sb, options);

            return sb.ToString();
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
    }
}
