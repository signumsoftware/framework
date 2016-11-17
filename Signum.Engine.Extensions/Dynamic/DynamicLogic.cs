using Signum.Engine;
using Signum.Engine.Authorization;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Dynamic;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
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
                DynamicLogic.GetCodeFiles += GetCodeGenStarter;
                AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(AssemblyResolveHandler);
            }
        }

        private static Assembly AssemblyResolveHandler(object sender, ResolveEventArgs args)
        {
            if (args.Name.StartsWith(DynamicLogic.CodeGenAssembly.Before(".")))
                return Assembly.LoadFrom(DynamicLogic.CodeGenAssemblyPath);

            return null;
        }

        public static void IncludeAllAssembliesAndNamespaces(SchemaBuilder sb)
        {
            var entitiesAndLogic = sb.Schema.Tables.Keys.Concat(sb.LoadedModules.Select(a => a.Item1));
            Namespaces.AddRange(entitiesAndLogic.Select(a => a.Namespace).Distinct().OrderBy().ToList());

            var assemblies = DirectedGraph<Assembly>.Generate(entitiesAndLogic.Select(a => a.Assembly).Distinct(), a => a.GetReferencedAssemblies().Select(ar => Assembly.Load(ar)));

            var assemblyNames = assemblies.Distinct().Select(a => a.GetName().Name + ".dll").Where(a => File.Exists(Path.Combine(Eval.AssemblyDirectory, a))).ToList();

            Assemblies.AddRange(assemblyNames);
        }

        public static HashSet<string> Namespaces = new HashSet<string>
        {
            "System",
            "System.Linq",
            "System.Reflection",
            "System.Collections.Generic",
            "System.Linq.Expressions",
            "Signum.Engine",
            "Signum.Entities",
            "Signum.Entities.Basics",
            "Signum.Engine.DynamicQuery",
            "Signum.Engine.Maps",
            "Signum.Engine.Basics",
            "Signum.Engine.Operations",
            "Signum.Utilities",
        };
        public static HashSet<string> Assemblies = new HashSet<string>
        {
            "Signum.Engine.dll",
            "Signum.Entities.dll",
            "Signum.Utilities.dll",
            "Signum.Entities.Extensions.dll",
            "Signum.Engine.Extensions.dll"
        };

        public static string CodeGenEntitiesNamespace = "Signum.Entities.CodeGen";
        public static string CodeGenDirectory = "CodeGen";
        public static string CodeGenAssembly = "DynamicAssembly.dll";
        public static string CodeGenAssemblyPath;

        public static Func<List<CodeFile>> GetCodeFiles = null;
        public static Action<StringBuilder, int> OnWriteDynamicStarter;
        public static Exception CodeGenError;




        public static void StartCodeGenStarter(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            try
            {
                Dictionary<string, CodeFile> codeFiles = GetCodeFilesDictionary();

                var cr = Compile(codeFiles, inMemory: false);

                if (cr == null)
                    return;

                if (cr.Errors.Count != 0)
                    throw new InvalidOperationException("Errors compiling  dynamic assembly:\r\n" + cr.Errors.Cast<CompilerError>().ToString("\r\n").Indent(4));

                CodeGenAssemblyPath = cr.PathToAssembly;

                Assembly assembly = cr.CompiledAssembly;
                Type type = assembly.GetTypes().Where(a => a.Name == "CodeGenStarter").SingleEx();
                MethodInfo mi = type.GetMethod("Start", BindingFlags.Public | BindingFlags.Static);
                mi.Invoke(null, new object[] { sb, dqm });
            }
            catch (Exception e)
            {
                e.LogException();
                CodeGenError = e;

                Console.WriteLine();
                SafeConsole.WriteLineColor(ConsoleColor.Red, "IMPORTANT!: Starting without Dynamic Entities.");
                SafeConsole.WriteLineColor(ConsoleColor.Yellow, "   Error:" + e.Message);
                SafeConsole.WriteLineColor(ConsoleColor.Red, "Synchronizing will try to DROP dynamic types. Clean the script manually!");
                Console.WriteLine();
            }
        }

        public static Dictionary<string, CodeFile> GetCodeFilesDictionary()
        {
            return GetCodeFiles.GetInvocationListTyped().SelectMany(f => f()).ToDictionaryEx(a => a.FileName, "C# code files");
        }

        public static CompilerResults Compile(Dictionary<string, CodeFile> codeFiles, bool inMemory)
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

                if (inMemory)
                    parameters.GenerateInMemory = true;
                else
                    parameters.OutputAssembly = Path.Combine(CodeGenDirectory, CodeGenAssembly);

              

                if (codeFiles.Count == 0)
                    return null;

                Directory.CreateDirectory(CodeGenDirectory);
                Directory.EnumerateFiles(CodeGenDirectory).Where(a => !inMemory || a != DynamicLogic.CodeGenAssemblyPath).ToList().ForEach(a => File.Delete(a));

                codeFiles.Values.ToList().ForEach(a => File.WriteAllText(Path.Combine(CodeGenDirectory, a.FileName), a.FileContent));

                CompilerResults compiled = supplier.CompileAssemblyFromFile(parameters, codeFiles.Values.Select(a => Path.Combine(CodeGenDirectory, a.FileName)).ToArray());

                return compiled;
            }
        }

        private static List<CodeFile> GetCodeGenStarter()
        {
            if (!Administrator.ExistTable<DynamicTypeEntity>())
                return new List<CodeFile>();

            var dscg = new DynamicStarterCodeGenerator(DynamicLogic.CodeGenEntitiesNamespace, Namespaces);

            var code = dscg.GetFileCode();

            var starter = new List<CodeFile>
                    {
                        new CodeFile
                        {
                            FileName = "CodeGenStarter.cs",
                            FileContent = code,
                        }
                    };

            return starter;
        }

        public class DynamicStarterCodeGenerator
        {
            public HashSet<string> Usings { get; private set; }
            public string Namespace { get; private set; }

            public DynamicStarterCodeGenerator(string @namespace, HashSet<string> usings)
            {
                this.Usings = usings;
                this.Namespace = @namespace;
            }

            public string GetFileCode()
            {
                StringBuilder sb = new StringBuilder();
                foreach (var item in this.Usings)
                    sb.AppendLine("using {0};".FormatWith(item));

                sb.AppendLine("[assembly: DefaultAssemblyCulture(\"en\")]");
                sb.AppendLine();
                sb.AppendLine("namespace " + this.Namespace);
                sb.AppendLine("{");
                sb.Append(GetStarterClassCode().Indent(4));
                sb.AppendLine("}");

                return sb.ToString();
            }

            public string GetStarterClassCode()
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine($"public static class CodeGenStarter");
                sb.AppendLine("{");
                sb.AppendLine("    public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)");
                sb.AppendLine("    {");
                DynamicLogic.OnWriteDynamicStarter(sb, 8);
                sb.AppendLine("    }");
                sb.AppendLine("}");

                return sb.ToString();
            }
        }
    }

    public class CodeFile
    {
        public string FileName; //Just for debugging
        public string FileContent;
    }
}
