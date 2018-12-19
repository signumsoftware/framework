using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Signum.Engine.Authorization;
using Signum.Engine.Basics;
using Signum.Engine.Maps;
using Signum.Entities.Basics;
using Signum.Entities.Dynamic;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Signum.Engine.Dynamic
{
    public static class DynamicLogic
    {
        public static void Start(SchemaBuilder sb)
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

        public static FileInfo GetLastCodeGenAssemblyFileInfo()
        {
            return new DirectoryInfo(DynamicCode.CodeGenDirectory)
                .GetFiles($"{DynamicCode.CodeGenAssembly.Before(".")}*.dll")
                .OrderByDescending(f => f.CreationTime)
                .FirstOrDefault();
        }

        public static FileInfo GetLoadedCodeGenAssemblyFileInfo()
        {
            if (DynamicCode.CodeGenAssemblyPath.IsNullOrEmpty())
                return null;

            return new DirectoryInfo(DynamicCode.CodeGenDirectory)
                .GetFiles(Path.GetFileName(DynamicCode.CodeGenAssemblyPath))
                .FirstOrDefault();
        }

        public static void BindCodeGenAssembly()
        {
            DynamicCode.CodeGenAssemblyPath = GetLastCodeGenAssemblyFileInfo()?.FullName;
        }

        public static void CompileDynamicCode()
        {
            try
            {
                Dictionary<string, CodeFile> codeFiles = GetCodeFilesDictionary();

                var cr = Compile(codeFiles, inMemory: false);

                if (cr.Errors.Count != 0)
                    throw new InvalidOperationException("Errors compiling  dynamic assembly:\r\n" + cr.Errors.ToString("\r\n").Indent(4));

                DynamicCode.CodeGenAssemblyPath = cr.OutputAssembly;
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

        public static void StartDynamicModules(SchemaBuilder sb)
        {
            if (CodeGenError != null)
                return;

            try
            {
                Assembly assembly = Assembly.LoadFrom(DynamicCode.CodeGenAssemblyPath);
                Type type = assembly.GetTypes().Where(a => a.Name == "CodeGenStarter").SingleEx();
                MethodInfo mi = type.GetMethod("Start", BindingFlags.Public | BindingFlags.Static);
                mi.Invoke(null, new object[] { sb });
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

        public class CompilationResult
        {
            public string OutputAssembly;
            public List<CompilationError> Errors;
        }

        public class CompilationError
        {
            public string FileName;
            public int Line;
            public int Column;
            public string ErrorNumber;
            public string ErrorText;
            public string FileContent;

            public override string ToString()
            {
                //CodeGen\CodeGenStarter.cs(58, 12): error CS0012: The type 'Attribute' is defined in an assembly that is not referenced. You must add a reference to assembly 'System.Runtime, Version=4.2.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'.
                return $"{FileName}({Line}:{Column}): error {ErrorNumber}: {ErrorText}";
            }
        }

        public static CompilationResult Compile(Dictionary<string, CodeFile> codeFiles, bool inMemory)
        {
            using (HeavyProfiler.Log("COMPILE"))
            {
                Directory.CreateDirectory(DynamicCode.CodeGenDirectory);

                try
                {
                    Directory.EnumerateFiles(DynamicCode.CodeGenDirectory)
                        .Where(a => a != DynamicCode.CodeGenAssemblyPath)
                        .ToList()
                        .ForEach(a => File.Delete(a));
                }
                catch (Exception)
                {
                    // Maybe we have Access denied exception to CodeGenAssembly*.dll
                }

                var utf8 = Encoding.UTF8;

                codeFiles.Values.ToList().ForEach(a => File.WriteAllText(Path.Combine(DynamicCode.CodeGenDirectory, a.FileName), a.FileContent, utf8));

                var references = DynamicCode.GetCoreMetadataReferences()
                    .Concat(DynamicCode.GetMetadataReferences(needsCodeGenAssembly: false));

                var compilation = CSharpCompilation.Create(Path.GetFileNameWithoutExtension(DynamicCode.CodeGenAssembly))
                      .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                      .AddReferences(references)
                      .AddSyntaxTrees(codeFiles.Values.Select(v => CSharpSyntaxTree.ParseText(v.FileContent, path: Path.Combine(DynamicCode.CodeGenDirectory, v.FileName))));

                DynamicCode.CodeGenGeneratedAssembly = $"{DynamicCode.CodeGenAssembly.Before(".")}.{Guid.NewGuid()}.dll";
                var outputAssembly = inMemory ? null : Path.Combine(DynamicCode.CodeGenDirectory, DynamicCode.CodeGenGeneratedAssembly);

                using (var stream = (Stream)new MemoryStream())
                {
                    var emitResult = compilation.Emit(stream);

                    if (emitResult.Success && !inMemory)
                    {
                        using (FileStream file = new FileStream(outputAssembly, FileMode.Create, FileAccess.ReadWrite))
                        {
                            stream.Position = 0;
                            stream.CopyTo(file);
                        }
                    }

                    return new CompilationResult
                    {
                        OutputAssembly = emitResult.Success ? outputAssembly : null,
                        Errors = emitResult.Diagnostics.Where(a => a.Severity == DiagnosticSeverity.Error)
                        .Select(d => new CompilationError
                        {
                            Column = d.Location.GetLineSpan().StartLinePosition.Character,
                            Line = d.Location.GetLineSpan().StartLinePosition.Line + 1,
                            FileContent = d.Location.SourceTree.ToString(),
                            FileName = d.Location.SourceTree.FilePath,
                            ErrorNumber = d.Descriptor.Id,
                            ErrorText = d.GetMessage(null)
                        })
                        .ToList()
                    };
                }
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
                sb.AppendLine("    public static void Start(SchemaBuilder sb)");
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
