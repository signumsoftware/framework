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
            var refs = References.Split(';').ToDictionary(a => Path.GetFileName(a));

            Log.LogMessage($"Reading {TemplateFile}");
            var file = File.ReadAllText(TemplateFile);

            Dictionary<string, string> parameters = new Dictionary<string, string>();

            file = Regex.Replace(file, @"//(?<key>\w*(\.\w*)?):\s*(?<value>.*?)\s*$\n", m =>
            {
                parameters.Add(m.Groups["key"].Value, m.Groups["value"].Value);
                return "";
            }, RegexOptions.Multiline);

            var references = Regex.Matches(file, @"import \* as (?<variable>\w+) from '.*?'").Cast<Match>().Select(a => a.Groups["variable"].Value)
                .Select(var => new Reference(refs.GetReferencedAssembly(parameters.ConsumeParameter(var + ".Assembly"), this.BuildEngine.ProjectFileOfTaskNode))
                {
                    VariableName = var,
                    BaseNamespace = parameters.ConsumeParameter(var + ".BaseNamespace"),
                }).ToList();

            var options = new Options(refs.GetReferencedAssembly(parameters.ConsumeParameter("Assembly"), this.BuildEngine.ProjectFileOfTaskNode))
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

            var targetFile = Path.ChangeExtension(TemplateFile, ".ts");
            Log.LogMessage($"Writing {targetFile}");
            File.WriteAllText(targetFile, sb.ToString());

            return true;
        }
    }

    public static class Bla
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
