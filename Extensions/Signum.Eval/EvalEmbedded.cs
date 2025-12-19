using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Runtime.Loader;

namespace Signum.Eval;

public abstract class EvalEmbedded<T> : EmbeddedEntity
                where T : class
{
    public class CompilationResult
    {
        public T Algorithm;
        public string CompilationErrors;
    }

    [Ignore, NonSerialized]
    CompilationResult? compilationResult;

    [HiddenProperty]
    public T Algorithm
    {
        get
        {
            CompileIfNecessary();
            
            if (compilationResult!.CompilationErrors != null)
                throw new InvalidOperationException(compilationResult.CompilationErrors);

            return compilationResult.Algorithm;
        }
    }

    [DbType(Size = int.MaxValue)]
    string script;
    [StringLengthValidator(MultiLine = true)]
    public string Script
    {
        get { return script; }
        set
        {
            if (Set(ref script, value))
                Reset();
        }
    }

    public void Reset()
    {
        compilationResult = null;
        Notify(() => Compiled);
    }

    protected abstract CompilationResult Compile();

    static ConcurrentDictionary<string, CompilationResult> resultCache = new ConcurrentDictionary<string, CompilationResult>();

    static EvalEmbedded()
    {
        EvalLogic.OnInvalidated += () => resultCache.Clear();
    }

    public static CompilationResult Compile(IEnumerable<MetadataReference> references, string code)
    {
        return resultCache.GetOrAdd(code, (Func<string, CompilationResult>)(_ =>
        {
            using (HeavyProfiler.Log("COMPILE", () => code))
            {
                try
                {
                    var tree = SyntaxFactory.ParseSyntaxTree(code);
                    
                    var compilation = CSharpCompilation.Create($"{Guid.NewGuid()}.dll")
                     .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable))
                     .AddReferences(references)
                     .AddSyntaxTrees(tree);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        var emit = compilation.Emit(ms);

                        if (!emit.Success)
                        {
                            var lines = code.Lines();
                            var errors = emit.Diagnostics.Where(a => a.DefaultSeverity == DiagnosticSeverity.Error);
                            return new CompilationResult
                            {
                                CompilationErrors = errors.Count() + " Errors:\n" + errors.ToString(e =>
                                {
                                    var line = e.Location.GetLineSpan().StartLinePosition.Line;
                                    return "Line {0}: {1}".FormatWith(line, e.GetMessage() + "\n" + lines[line]);
                                }, "\n\n")
                            };
                        }

                        if (EvalLogic.GetCustomErrors != null)
                        {
                            var allCustomErrors = EvalLogic.GetCustomErrors.GetInvocationListTyped()
                            .SelectMany((Func<string, List<CustomCompilerError>> a) => a(code) ?? Enumerable.Empty<CustomCompilerError>()).ToList();

                            if (allCustomErrors.Any())
                            {
                                var lines = code.Split('\n');
                                return new CompilationResult
                                {
                                    CompilationErrors = allCustomErrors.Count + " Errors:\n" + allCustomErrors.ToString(e => {
                                        return "Line {0}: {1}".FormatWith(e.Line, e.ErrorText) + "\n" + lines[e.Line - 1];
                                    }, "\n\n")
                                };
                            }
                        }

                        ms.Seek(0, SeekOrigin.Begin);
                        var assembly = AssemblyLoadContext.Default.LoadFromStream(ms);

                        Type type = assembly.GetTypes().Where(a => typeof(T).IsAssignableFrom(a)).SingleEx();

                        T algorithm = (T)assembly.CreateInstance(type.FullName!)!;

                        return new CompilationResult { Algorithm = algorithm };
                    }
                }
                catch (Exception e)
                {
                    return new CompilationResult { CompilationErrors = e.Message };
                }
            }
        }));
    }

    [HiddenProperty]
    public bool Compiled
    {
        get { return compilationResult != null; }
    }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(Script) && compilationResult != null)
            return compilationResult.CompilationErrors;

        return null;
    }

    protected override void PreSaving(PreSavingContext ctx)
    {
        CompileIfNecessary();

        base.PreSaving(ctx);
    }

    private void CompileIfNecessary()
    {
        if (compilationResult == null && Script.HasText())
        {
            compilationResult = Compile();
            Notify(() => Compiled);
        }
    }
}

[AutoInit]
public static class EvalPanelPermission
{
    public static PermissionSymbol ViewDynamicPanel;
}

public enum EvalPanelMessage
{
    OpenErrors,
    DynamicPanel,
    Search,
    CheckEvals,
    RefreshAll,
    NoErrorsFound,
    [Description("{0} found")]
    _0Found,
    [Description("Exception checking {0}")]
    ExceptionChecking0_,
    [Description("Now you need to refresh the clients manually (i.e. pressing F5).")]
    YouNeedToRefreshManually,
    RefreshThisClient,
}
