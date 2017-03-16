using Signum.Engine;
using Signum.Engine.Authorization;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Basics;
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
            if (args.Name.StartsWith(DynamicCode.CodeGenAssembly.Before(".")))
                return Assembly.LoadFrom(DynamicCode.CodeGenAssemblyPath);

            return null;
        }

        public static Func<List<CodeFile>> GetCodeFiles = null;
        public static Action<StringBuilder, int> OnWriteDynamicStarter;
        public static Exception CodeGenError;

        public static void CompileDynamicCode()
        {
            try
            {
                Dictionary<string, CodeFile> codeFiles = GetCodeFilesDictionary();

                var cr = Compile(codeFiles, inMemory: false);

                if (cr.Errors.Count != 0)
                    throw new InvalidOperationException("Errors compiling  dynamic assembly:\r\n" + cr.Errors.Cast<CompilerError>().ToString("\r\n").Indent(4));

                DynamicCode.CodeGenAssemblyPath = cr.PathToAssembly;
            }
            catch (Exception e)
            {
                CodeGenError = e;
            }
        }

        public static void RegisterExceptionIfAny()
        {
            var e = CodeGenError;
            if (e == null)
                return;

            if (Administrator.ExistsTable<ExceptionEntity>())
                e.LogException();

            Console.WriteLine();
            SafeConsole.WriteLineColor(ConsoleColor.Red, "IMPORTANT!: Starting without Dynamic Entities.");
            SafeConsole.WriteLineColor(ConsoleColor.Yellow, "   Error:" + e.Message);
            SafeConsole.WriteLineColor(ConsoleColor.Red, "Synchronizing will try to DROP dynamic types. Clean the script manually!");
            Console.WriteLine();
        }

        public static void RegisterMixins()
        {
            if (CodeGenError != null)
                return;

            try
            {
                Assembly assembly = Assembly.LoadFrom(DynamicCode.CodeGenAssemblyPath);
                Type type = assembly.GetTypes().Where(a => a.Name == "CodeGenMixinLogic").SingleEx();
                MethodInfo mi = type.GetMethod("Start", BindingFlags.Public | BindingFlags.Static);
                mi.Invoke(null, null);
            }
            catch (Exception e)
            {
                CodeGenError = e.InnerException;
            }
        }

        public static void BeforeSchema(SchemaBuilder sb)
        {
            if (CodeGenError != null)
                return;

            try
            {
                Assembly assembly = Assembly.LoadFrom(DynamicCode.CodeGenAssemblyPath);
                Type type = assembly.GetTypes().Where(a => a.Name == "CodeGenBeforeSchemaLogic").SingleEx();
                MethodInfo mi = type.GetMethod("Start", BindingFlags.Public | BindingFlags.Static);
                mi.Invoke(null, new[] { sb });
            }
            catch (Exception e)
            {
                CodeGenError = e.InnerException;
            }
        }

        public static void StartDynamicModules(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (CodeGenError != null)
                return;

            try
            {
                Assembly assembly = Assembly.LoadFrom(DynamicCode.CodeGenAssemblyPath);
                Type type = assembly.GetTypes().Where(a => a.Name == "CodeGenStarter").SingleEx();
                MethodInfo mi = type.GetMethod("Start", BindingFlags.Public | BindingFlags.Static);
                mi.Invoke(null, new object[] { sb, dqm });
            }
            catch (Exception e)
            {
                CodeGenError = e.InnerException;
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
                parameters.ReferencedAssemblies.Add("System.Data.dll");
                parameters.ReferencedAssemblies.Add("System.Core.dll");
                foreach (var ass in DynamicCode.Assemblies)
                {
                    parameters.ReferencedAssemblies.Add(Path.Combine(DynamicCode.AssemblyDirectory, ass));
                }

                if (inMemory)
                    parameters.GenerateInMemory = true;
                else
                    parameters.OutputAssembly = Path.Combine(DynamicCode.CodeGenDirectory, DynamicCode.CodeGenAssembly);

                Directory.CreateDirectory(DynamicCode.CodeGenDirectory);
                Directory.EnumerateFiles(DynamicCode.CodeGenDirectory).Where(a => !inMemory || a != DynamicCode.CodeGenAssemblyPath).ToList().ForEach(a => File.Delete(a));

                codeFiles.Values.ToList().ForEach(a => File.WriteAllText(Path.Combine(DynamicCode.CodeGenDirectory, a.FileName), a.FileContent));

                CompilerResults compiled = supplier.CompileAssemblyFromFile(parameters, codeFiles.Values.Select(a => Path.Combine(DynamicCode.CodeGenDirectory, a.FileName)).ToArray());

                return compiled;
            }
        }

        private static List<CodeFile> GetCodeGenStarter()
        {
            var dscg = new DynamicStarterCodeGenerator(DynamicCode.CodeGenEntitiesNamespace, DynamicCode.Namespaces);

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
