using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Diagnostics;
using Mono.Cecil;
using System.CodeDom.Compiler;
using Mono.Cecil.Pdb;

namespace Signum.TSGenerator
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            Stopwatch sw = Stopwatch.StartNew();

            string intermediateAssembly = args[0];
            string[] references = File.ReadAllLines(args[1]);
            string[] content = File.ReadAllLines(args[2]);

            var log = Console.Out;

            log.WriteLine("Starting SignumTSGenerator");

            bool hasPdb = File.Exists(Path.ChangeExtension(intermediateAssembly, ".pdb"));

            AssemblyDefinition reactAssembly = AssemblyDefinition.ReadAssembly(intermediateAssembly, new ReaderParameters
            {
                ReadingMode = ReadingMode.Deferred,
                ReadSymbols = hasPdb,
                InMemory = true,
                SymbolReaderProvider = hasPdb ? new PdbReaderProvider() : null
            });


            if (AlreadyProcessed(reactAssembly))
            {
                log.WriteLine("SignumTSGenerator already processed: {0}", intermediateAssembly);
                return 0;
            }

            PreloadingAssemblyResolver resolver = new PreloadingAssemblyResolver(references);

            var assemblyReferences = (from r in references
                                      where r.Contains(".Entities")
                                      let reactDirectory = FindReactDirectory(r)
                                      select new AssemblyReference
                                      {
                                          AssemblyName = Path.GetFileNameWithoutExtension(r),
                                          AssemblyFullPath = r,
                                          ReactDirectory = reactDirectory,
                                          AllTypescriptFiles = GetAllTypescriptFiles(reactDirectory),
                                      }).ToDictionary(a => a.AssemblyName);

            var entitiesAssembly = Path.GetFileNameWithoutExtension(intermediateAssembly).Replace(".React", ".Entities");
            var entitiesAssemblyReference = assemblyReferences.GetOrThrow(entitiesAssembly);
            var entitiesModule = ModuleDefinition.ReadModule(entitiesAssemblyReference.AssemblyFullPath, new ReaderParameters { AssemblyResolver = resolver });
            var options = new AssemblyOptions
            {
                CurrentAssembly = entitiesAssembly,
                AssemblyReferences = assemblyReferences,
                AllReferences = references.ToDictionary(a => Path.GetFileNameWithoutExtension(a)),
                ModuleDefinition = entitiesModule,
                Resolver = resolver,
            };


            var currentDir = Directory.GetCurrentDirectory();
            var files = content
                .Where(file => Path.GetExtension(file) == ".t4s")
                .Select(file => Path.Combine(currentDir, file))
                .ToList();

            bool hasErrors = false;
            foreach (var file in files)
            {
                try
                {
                    string result = EntityDeclarationGenerator.Process(options, file, Path.GetFileNameWithoutExtension(file));

                    var targetFile = Path.ChangeExtension(file, ".ts");
                    if (File.Exists(targetFile) && File.ReadAllText(targetFile) == result)
                    {
                        log.WriteLine($"Skipping {targetFile} (Up to date)");
                    }
                    else
                    {
                        log.WriteLine($"Writing {targetFile}");
                        File.WriteAllText(targetFile, result);
                    }
                }
                catch (Exception ex)
                {
                    hasErrors = true;
                    log.WriteLine($"Error in {file}");
                    log.WriteLine(ex.Message);
                }
            }

            MarkAsProcessed(reactAssembly, resolver);

            reactAssembly.Write(intermediateAssembly, new WriterParameters
            {
                WriteSymbols = hasPdb,
                SymbolWriterProvider = hasPdb ? new PdbWriterProvider() : null
            });

            log.WriteLine($"SignumTSGenerator finished in {sw.ElapsedMilliseconds.ToString()}ms");

            Console.WriteLine();

            return hasErrors ? -1 : 0;
        }

        static bool AlreadyProcessed(AssemblyDefinition assembly)
        {
            var nameof = typeof(GeneratedCodeAttribute).FullName;
            var attr = assembly.CustomAttributes
                .Any(a => a.AttributeType.FullName == nameof && ((string)a.ConstructorArguments[0].Value) == "SignumTask");

            return attr;
        }

        static void MarkAsProcessed(AssemblyDefinition assembly, IAssemblyResolver resolver)
        {
            TypeDefinition generatedCodeAttribute = resolver.Resolve(AssemblyNameReference.Parse(typeof(GeneratedCodeAttribute).Assembly.GetName().Name)).MainModule.GetType(typeof(GeneratedCodeAttribute).FullName);
            MethodDefinition constructor = generatedCodeAttribute.Methods.Single(a => a.IsConstructor && a.Parameters.Count == 2);

            TypeReference stringType = assembly.MainModule.TypeSystem.String;
            assembly.CustomAttributes.Add(new CustomAttribute(assembly.MainModule.ImportReference(constructor))
            {
                ConstructorArguments =
                {
                    new CustomAttributeArgument(stringType, "SignumTask"),
                    new CustomAttributeArgument(stringType, typeof(Program).Assembly.GetName().Version.ToString()),
                }
            });
        }


        static string FindReactDirectory(string absoluteFilePath)
        {
            var prefix = absoluteFilePath;
            while (prefix != null)
            {
                var name = Path.GetFileName(prefix);

                if (name.Contains(".Entities"))
                {
                    name = name.Replace(".Entities", ".React");
                    var dir = Path.Combine(Path.GetDirectoryName(prefix), name);
                    if (Directory.Exists(dir))
                        return dir;
                }

                prefix = Path.GetDirectoryName(prefix);
            }

            throw new InvalidOperationException("Impossible to determine the react directory for '" + absoluteFilePath + "'");
        }

        public static List<string> GetAllTypescriptFiles(string reactDirectory)
        {
            return new DirectoryInfo(reactDirectory).EnumerateFiles("*.ts", SearchOption.AllDirectories)
                .Concat(new DirectoryInfo(reactDirectory).EnumerateFiles("*.t4s", SearchOption.AllDirectories))
                .Select(a => a.FullName)
                .Where(fn => !fn.Contains(@"\obj\") && !fn.Contains(@"\bin\")) //Makes problem when deploying
                .ToList();
        }
    }
}
