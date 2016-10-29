using Signum.Engine;
using Signum.Engine.Authorization;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Dynamic;
using Signum.Utilities;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.Dynamic
{
    public static class DynamicLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                PermissionAuthLogic.RegisterTypes(typeof(DynamicPanelPermission));
            }
        }

        public static List<string> Assemblies = Eval.BasicAssemblies;
        public static string GeneratedCodeDirectory = "DynamicallyGeneratedCode";

        public static Func<List<CodeFile>> GetCodeFiles = null;

        public static void StartDynamicStarter(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            Dictionary<string, CodeFile> codeFiles;

            var cr = Compile(out codeFiles);
            if (cr.Errors.Count != 0)
                throw new InvalidOperationException("Errors compiling  dynamic assembly:\r\n" + cr.Errors.Cast<CompilerError>().ToString("\r\n").Indent(4));
            
            Assembly assembly = cr.CompiledAssembly;
            Type type = assembly.GetTypes().Where(a => a.Name == "DynamicStarter").SingleEx();
            MethodInfo mi = type.GetMethod("Start", BindingFlags.Public | BindingFlags.Static);
            mi.Invoke(null, new object[] { sb, dqm });
        }

        public static CompilerResults Compile(out Dictionary<string, CodeFile> codeFiles)
        {
            using (HeavyProfiler.Log("COMPILE"))
            {
                CodeDomProvider supplier = new Microsoft.CodeDom.Providers.DotNetCompilerPlatform.CSharpCodeProvider();

                CompilerParameters parameters = new CompilerParameters();

                parameters.ReferencedAssemblies.Add("System.dll");
                parameters.ReferencedAssemblies.Add("System.Core.dll");
                foreach (var ass in Assemblies)
                {
                    parameters.ReferencedAssemblies.Add(Path.Combine(Eval.AssemblyDirectory, ass));
                }

                parameters.GenerateInMemory = true;

                codeFiles = GetCodeFiles.GetInvocationListTyped().SelectMany(f => f()).ToDictionary(a => a.FileName, "C# code files");

                Directory.CreateDirectory(GeneratedCodeDirectory);
                Directory.EnumerateFiles(GeneratedCodeDirectory).ToList().ForEach(a => File.Delete(a));

                codeFiles.Values.ToList().ForEach(a => File.WriteAllText(Path.Combine(GeneratedCodeDirectory, a.FileName), a.FileContent));

                CompilerResults compiled = supplier.CompileAssemblyFromFile(parameters, codeFiles.Values.Select(a => Path.Combine(GeneratedCodeDirectory, a.FileName)).ToArray());

                return compiled;
            }
        }
    }

    public class CodeFile
    {
        public string FileName; //Just for debugging
        public string FileContent;
    }
}
