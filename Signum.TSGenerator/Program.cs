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
            var log = Console.Out;

            Stopwatch sw = Stopwatch.StartNew();

            string intermediateAssembly = args[0];
            string[] references = File.ReadAllLines(args[1]);
            string[] content = File.ReadAllLines(args[2]);

            log.WriteLine("Starting SignumTSGenerator");

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
          
            var currentDir = Directory.GetCurrentDirectory();
            var files = content
                .Where(file => Path.GetExtension(file) == ".t4s")
                .Select(file => Path.Combine(currentDir, file))
                .ToList();

            var upToDateContent = string.Join("\r\n",
                 new[] { entitiesAssemblyReference.AssemblyFullPath }
                 .Concat(files)
                 .OrderBy(a => a)
                 .Select(f => File.GetLastWriteTimeUtc(f).ToString("o") + " " + Path.GetFileName(f)));

            var signumUpToDatePath = Path.Combine(Path.GetDirectoryName(args[1]), "SignumUpToDate.txt");

            if(File.Exists(signumUpToDatePath) && File.ReadAllText(signumUpToDatePath) == upToDateContent)
            {
                log.WriteLine($"SignumTSGenerator already processed ({sw.ElapsedMilliseconds.ToString()}ms)");
                return 0;
            }

            var entityResolver = new PreloadingAssemblyResolver(references);
            var entitiesModule = ModuleDefinition.ReadModule(entitiesAssemblyReference.AssemblyFullPath, new ReaderParameters
            {
                AssemblyResolver = entityResolver
            });

            var options = new AssemblyOptions
            {
                CurrentAssembly = entitiesAssembly,
                AssemblyReferences = assemblyReferences,
                AllReferences = references.ToDictionary(a => Path.GetFileNameWithoutExtension(a)),
                ModuleDefinition = entitiesModule,
                Resolver = entityResolver,
            };

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
                    log.WriteLine($"{file}:error STSG0001:{ex.Message}");
                    log.WriteLine(ex.Message);
                }
            }

            if (hasErrors)
            {
                log.WriteLine($"SignumTSGenerator finished with errors ({sw.ElapsedMilliseconds.ToString()}ms)");
                Console.WriteLine();
                return 0;
            }
            else
            {
                File.WriteAllText(signumUpToDatePath, upToDateContent);
                log.WriteLine($"SignumTSGenerator finished ({sw.ElapsedMilliseconds.ToString()}ms)");
                Console.WriteLine();
                return 0;
            }
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
