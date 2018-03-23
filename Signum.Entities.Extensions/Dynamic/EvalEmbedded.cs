using Microsoft.CSharp;
using Signum.Entities;
using Signum.Utilities;
using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Dynamic
{
    [Serializable]
    public abstract class EvalEmbedded<T> : EmbeddedEntity
                    where T : class
    {
        public class CompilationResult
        {
            public T Algorithm;
            public string CompilationErrors;
        }

        [Ignore, NonSerialized]
        CompilationResult compilationResult;

        [HiddenProperty]
        public T Algorithm
        {
            get
            {
                CompileIfNecessary();

                if (compilationResult == null)
                    return null;

                if (compilationResult.CompilationErrors != null)
                    throw new InvalidOperationException(compilationResult.CompilationErrors);

                return compilationResult.Algorithm;
            }
        }

        [SqlDbType(Size = int.MaxValue)]
        string script;
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


        public static CompilationResult Compile(IEnumerable<string> assemblies, string code)
        {
            return resultCache.GetOrAdd(code, _ =>
            {
                using (HeavyProfiler.Log("COMPILE", () => code))
                {
                    try
                    {
                        CodeDomProvider supplier = new Microsoft.CodeDom.Providers.DotNetCompilerPlatform.CSharpCodeProvider();

                        CompilerParameters parameters = new CompilerParameters();

                        parameters.ReferencedAssemblies.Add("System.dll");
                        parameters.ReferencedAssemblies.Add("System.Data.dll");
                        parameters.ReferencedAssemblies.Add("System.Core.dll");
                        
                        foreach (var ass in assemblies)
                        {
                            parameters.ReferencedAssemblies.Add(ass);
                        }

                        parameters.GenerateInMemory = true;

                        CompilerResults compiled = supplier.CompileAssemblyFromSource(parameters, code);

                        if (compiled.Errors.HasErrors)
                        {
                            var lines = code.Split('\n');
                            var errors = compiled.Errors.Cast<CompilerError>();
                            return new CompilationResult
                            {
                                CompilationErrors = errors.Count() + " Errors:\r\n" + errors.ToString(e => "Line {0}: {1}".FormatWith(e.Line, e.ErrorText) + "\r\n" + lines[e.Line - 1], "\r\n\r\n")
                            };
                        }

                        if (DynamicCode.GetCustomErrors != null)
                        {
                            var allCustomErrors = DynamicCode.GetCustomErrors.GetInvocationListTyped().SelectMany(a => a(code) ?? new List<CompilerError>()).ToList();
                            if (allCustomErrors.Any())
                            {
                                var lines = code.Split('\n');
                                return new CompilationResult
                                {
                                    CompilationErrors = allCustomErrors.Count() + " Errors:\r\n" + allCustomErrors.ToString(e => "Line {0}: {1}".FormatWith(e.Line, e.ErrorText) + "\r\n" + lines[e.Line - 1], "\r\n\r\n")
                                };
                            }
                        }

                        Assembly assembly = compiled.CompiledAssembly;
                        Type type = assembly.GetTypes().Where(a => typeof(T).IsAssignableFrom(a)).SingleEx();

                        T algorithm = (T)assembly.CreateInstance(type.FullName);

                        return new CompilationResult { Algorithm = algorithm };

                    }
                    catch (Exception e)
                    {
                        return new CompilationResult { CompilationErrors = e.Message };
                    }
                }
            });
        }

        [HiddenProperty]
        public bool Compiled
        {
            get { return compilationResult != null; }
        }

        protected override string PropertyValidation(PropertyInfo pi)
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
}
