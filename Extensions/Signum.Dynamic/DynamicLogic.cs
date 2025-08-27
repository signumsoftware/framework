using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Signum.Dynamic.Controllers;
using Signum.Dynamic;
using Signum.Eval;
using System.IO;
using Signum.API;
using Signum.Eval.TypeHelp;

namespace Signum.Dynamic;

public static class DynamicLogic
{
    public static string CodeGenNamespace = "Signum.CodeGen";
    public static string CodeGenDirectory = "CodeGen";
    public static string CodeGenAssembly = "CodeGenAssembly.dll";
    public static string CodeGenControllerAssembly = "CodeGenControllerAssembly.dll";
    public static string? CodeGenAssemblyPath;
    public static string? CodeGenControllerAssemblyPath;
    public static Action OnApplicationServerRestarted;


    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            TypeHelpLogic.Start(sb);
            PermissionLogic.RegisterPermissions(DynamicPanelPermission.RestartApplication);
            DynamicLogic.GetCodeFiles += GetCodeGenStarter;
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(AssemblyResolveHandler);
            EvalLogic.Namespaces.Add(CodeGenNamespace);
            EvalLogic.GetCodeGenAssemblyPath = () => CodeGenAssemblyPath;

            if (sb.WebServerBuilder != null)
                DynamicServer.Start(sb.WebServerBuilder);
        }
    }

    private static Assembly AssemblyResolveHandler(object? sender, ResolveEventArgs args)
    {
        if (args.Name!.StartsWith(DynamicLogic.CodeGenAssembly.Before(".")))
            return Assembly.LoadFrom(CodeGenAssemblyPath!);

        return null!;
    }

    public static Func<List<CodeFile>>? GetCodeFiles;
    public static Action<StringBuilder, int>? OnWriteDynamicStarter;
    public static Exception? CodeGenError;

    public static FileInfo? GetLastCodeGenAssemblyFileInfo()
    {
        Directory.CreateDirectory(DynamicLogic.CodeGenDirectory);

        return new DirectoryInfo(DynamicLogic.CodeGenDirectory)
            .GetFiles($"{DynamicLogic.CodeGenAssembly.Before(".")}*.dll")
            .OrderByDescending(f => f.CreationTime)
            .FirstOrDefault();
    }

    public static FileInfo? GetLastCodeGenControllerAssemblyFileInfo()
    {
        Directory.CreateDirectory(CodeGenDirectory);

        return new DirectoryInfo(CodeGenDirectory)
            .GetFiles($"{CodeGenControllerAssembly.Before(".")}*.dll")
            .OrderByDescending(f => f.CreationTime)
            .FirstOrDefault();
    }

    public static FileInfo? GetLoadedCodeGenAssemblyFileInfo()
    {
        if (CodeGenAssemblyPath.IsNullOrEmpty())
            return null;

        return new DirectoryInfo(CodeGenDirectory)
            .GetFiles(Path.GetFileName(CodeGenAssemblyPath))
            .FirstOrDefault();
    }

    public static FileInfo? GetLoadedCodeGenControllerAssemblyFileInfo()
    {
        if (CodeGenControllerAssemblyPath.IsNullOrEmpty())
            return null;

        return new DirectoryInfo(CodeGenDirectory)
            .GetFiles(Path.GetFileName(CodeGenControllerAssemblyPath))
            .FirstOrDefault();
    }

    private static void BindCodeGenAssemblies()
    {
        CodeGenAssemblyPath = GetLastCodeGenAssemblyFileInfo()?.FullName;
        CodeGenControllerAssemblyPath = GetLastCodeGenControllerAssemblyFileInfo()?.FullName;
    }

    public static void CompileDynamicCode()
    {
        DynamicLogic.BindCodeGenAssemblies();
        Directory.CreateDirectory(CodeGenDirectory);

        var errors = new List<string>();
        try
        {
            CompilationResult? cr = null;

            bool cleaned = false;
            if (CodeGenAssemblyPath.IsNullOrEmpty())
            {
                CleanCodeGenFolder();
                cleaned = true;

                {
                    Dictionary<string, CodeFile> codeFiles = GetCodeFilesDictionary();

                    cr = Compile(codeFiles, inMemory: false, assemblyName: CodeGenAssembly, needsCodeGenAssembly: false);

                    if (cr.Errors.Count == 0)
                        CodeGenAssemblyPath = cr.OutputAssembly;
                    else
                        errors.Add("Errors compiling  dynamic assembly:\r\n" + cr.Errors.ToString("\r\n").Indent(4));
                }
            }

            if (DynamicApiLogic.IsStarted && (CodeGenControllerAssemblyPath.IsNullOrEmpty() || cleaned))
            {
                Dictionary<string, CodeFile> codeFiles = DynamicApiLogic.GetCodeFiles().ToDictionary(a => a.FileContent);
                cr = Compile(codeFiles, inMemory: false, assemblyName: CodeGenControllerAssembly, needsCodeGenAssembly: true);

                if (cr.Errors.Count == 0)
                    CodeGenControllerAssemblyPath = cr.OutputAssembly;
                else
                    errors.Add("Errors compiling  dynamic api controller assembly:\r\n" + cr.Errors.ToString("\r\n").Indent(4));
            }

            if (errors.Any())
                throw new InvalidOperationException(errors.ToString("\r\n"));
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

        Schema.Current.Initializing += () =>
        {
            if (Administrator.ExistsTable<ExceptionEntity>())
                e.LogException();
        };

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
            Assembly assembly = Assembly.LoadFrom(CodeGenAssemblyPath!);
            Type type = assembly.GetTypes().Where(a => a.Name == "CodeGenMixinLogic").SingleEx();
            MethodInfo mi = type.GetMethod("Start", BindingFlags.Public | BindingFlags.Static)!;
            mi.Invoke(null, null);
        }
        catch (Exception e)
        {
            CodeGenError = e.InnerException;
        }
    }

    public static void RegisterIsolations()
    {
        if (CodeGenError != null)
            return;

        try
        {
            Assembly assembly = Assembly.LoadFrom(CodeGenAssemblyPath!);
            Type type = assembly.GetTypes().Where(a => a.Name == "CodeGenIsolationLogic").SingleEx();
            MethodInfo mi = type.GetMethod("Start", BindingFlags.Public | BindingFlags.Static)!;
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
            Assembly assembly = Assembly.LoadFrom(CodeGenAssemblyPath!);
            Type type = assembly.GetTypes().Where(a => a.Name == "CodeGenBeforeSchemaLogic").SingleEx();
            MethodInfo mi = type.GetMethod("Start", BindingFlags.Public | BindingFlags.Static)!;
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
            Assembly assembly = Assembly.LoadFrom(CodeGenAssemblyPath!);
            Type type = assembly.GetTypes().Where(a => a.Name == "CodeGenStarter").SingleEx();
            MethodInfo mi = type.GetMethod("Start", BindingFlags.Public | BindingFlags.Static)!;
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
        public string? OutputAssembly;
        public List<CompilationError> Errors;

        public CompilationResult(string? outputAssembly, List<CompilationError> errors)
        {
            OutputAssembly = outputAssembly;
            Errors = errors;
        }
    }

    public class CompilationError
    {
        public string FileName;
        public int Line;
        public int Column;
        public string ErrorNumber;
        public string ErrorText;
        public string FileContent;

        public CompilationError(Diagnostic d)
        {
            this.Column = d.Location.GetLineSpan().StartLinePosition.Character;
            this.Line = d.Location.GetLineSpan().StartLinePosition.Line + 1;
            this.FileContent = d.Location.SourceTree!.ToString();
            this.FileName = d.Location.SourceTree.FilePath;
            this.ErrorNumber = d.Descriptor.Id;
            this.ErrorText = d.GetMessage(null);
        }

        public override string ToString()
        {
            //CodeGen\CodeGenStarter.cs(58, 12): error CS0012: The type 'Attribute' is defined in an assembly that is not referenced. You must add a reference to assembly 'System.Runtime, Version=4.2.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'.
            return $"{FileName}({Line}:{Column}): error {ErrorNumber}: {ErrorText}";
        }
    }

    public static void CleanCodeGenFolder()
    {
        try
        {
            Directory.EnumerateFiles(CodeGenDirectory)
                .ToList()
                .ForEach(a => File.Delete(a));
        }
        catch (Exception)
        {
            // Maybe we have no access to delete CodeGenAssembly*.dll or CodeGenControllerAssembly*.dll
        }
    }

    public static CompilationResult Compile(Dictionary<string, CodeFile> codeFiles, 
        bool inMemory, 
        string assemblyName, 
        bool needsCodeGenAssembly)
    {
        using (HeavyProfiler.Log("COMPILE"))
        {
            if (!inMemory)
            {
                codeFiles.Values.ToList().ForEach(a => File.WriteAllText(Path.Combine(CodeGenDirectory, a.FileName), a.FileContent, Encoding.UTF8));
            }

            var references = EvalLogic.GetCoreMetadataReferences()
                .Concat(EvalLogic.GetMetadataReferences(needsCodeGenAssembly));

            var compilation = CSharpCompilation.Create(Path.GetFileNameWithoutExtension(assemblyName))
                  .WithOptions(new CSharpCompilationOptions(outputKind: OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable))
                  .AddReferences(references)
                  .AddSyntaxTrees(codeFiles.Values.Select(v => CSharpSyntaxTree.ParseText(v.FileContent, path: Path.Combine(CodeGenDirectory, v.FileName), options: new CSharpParseOptions(LanguageVersion.CSharp8))));

            var outputAssembly = inMemory ? null : Path.Combine(CodeGenDirectory, $"{assemblyName.Before(".")}.{Guid.NewGuid()}.dll");

            using (var stream = new MemoryStream())
            {
                var emitResult = compilation.Emit(stream);

                if (emitResult.Success && !inMemory)
                {
                    using (FileStream file = new FileStream(outputAssembly!, FileMode.Create, FileAccess.ReadWrite))
                    {
                        stream.Position = 0;
                        stream.CopyTo(file);
                    }
                }

                var errors = emitResult.Diagnostics.Where(a => a.Severity == DiagnosticSeverity.Error)
                    .Select(d => new CompilationError(d)).ToList();

                return new CompilationResult(
                    outputAssembly: emitResult.Success ? outputAssembly : null,
                    errors: errors
                );
            }
        }
    }

    private static List<CodeFile> GetCodeGenStarter()
    {
        var dscg = new DynamicStarterCodeGenerator(CodeGenNamespace, EvalLogic.Namespaces);

        var code = dscg.GetFileCode();

        var starter = new List<CodeFile>
        {
            new CodeFile( "CodeGenStarter.cs",code)
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
            DynamicLogic.OnWriteDynamicStarter?.Invoke(sb, 8);
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

    public CodeFile(string fileName, string fileContent)
    {
        FileName = fileName;
        FileContent = fileContent;
    }
}
