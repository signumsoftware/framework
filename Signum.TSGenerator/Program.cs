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
using System.Xml.Linq;
using System.Reflection.PortableExecutable;

namespace Signum.TSGenerator;

public static class Program
{
    public static int Main(string[] args)
    {
        var log = Console.Out;
        Stopwatch sw = Stopwatch.StartNew();
        string currentCsproj = "";
        try
        {
            string intermediateAssembly = args[0];
            string referencesFile = args[1];

            var currentDirectory = Directory.GetCurrentDirectory();

            currentCsproj = Path.Combine(currentDirectory, Path.GetFileNameWithoutExtension(intermediateAssembly) + ".csproj");
            if (!File.Exists(currentCsproj))
                throw new InvalidOperationException($"Project file not found in ({currentCsproj})");

            log.WriteLine("Starting SignumTSGenerator");

            var currentT4Files = GetAllT4SFiles(currentDirectory);
            var signumUpToDatePath = Path.Combine(Path.GetDirectoryName(intermediateAssembly), "SignumUpToDate.txt");

            if (File.Exists(signumUpToDatePath))
            {
                var upToDateContent = GetUpToDateContent(intermediateAssembly, currentT4Files);
                if (File.ReadAllText(signumUpToDatePath) == upToDateContent) {

                    log.WriteLine($"SignumTSGenerator already processed ({sw.ElapsedMilliseconds.ToString()}ms)");
                    return 0;
                }
            }

            string[] references = File.ReadAllLines(referencesFile);
            var assemblyLocations = references.ToDictionary(a => Path.GetFileNameWithoutExtension(a));

            assemblyLocations.Add(Path.GetFileNameWithoutExtension(intermediateAssembly), Path.Combine(Directory.GetCurrentDirectory(), intermediateAssembly));

            var candidates = GetProjectReferences(currentCsproj).Prepend(currentCsproj).ToList();

            var assemblyReferences = (from csproj in candidates
                                      where ReferencesOrIsSignum(csproj)
                                      let dir = Path.GetDirectoryName(csproj)
                                      let assemblyName = Path.GetFileNameWithoutExtension(csproj)
                                      select new AssemblyReference
                                      {
                                          AssemblyName = assemblyName,
                                          //AssemblyFullPath = assemblyLocations[assemblyName],
                                          Directory = dir,
                                          AllTypescriptFiles = GetAllT4SFiles(Path.Combine(Directory.GetCurrentDirectory(), dir)),
                                      }).ToDictionary(a => a.AssemblyName);


            var entityResolver = new PreloadingAssemblyResolver(assemblyLocations);
            var currentModule = ModuleDefinition.ReadModule(intermediateAssembly, new ReaderParameters
            {
                AssemblyResolver = entityResolver
            });

            var options = new AssemblyOptions
            {
                CurrentAssembly = intermediateAssembly,
                AssemblyReferences = assemblyReferences,
                AllReferences = references.ToDictionary(a => Path.GetFileNameWithoutExtension(a)),
                ModuleDefinition = currentModule,
                Resolver = entityResolver,
            };


            var tsTypes = EntityDeclarationGenerator.GetAllTSTypes(options);

            var shouldT4s = tsTypes.GroupBy(a => a.Namespace).ToDictionary(gr => gr.Key, gr => gr.ToList());
            var currentT4s = currentT4Files.ToDictionary(a => Path.GetFileNameWithoutExtension(a));

            var extra = currentT4s.Where(kvp => !shouldT4s.ContainsKey(kvp.Key)).ToList();

            if (extra.Any())
            {
                log.WriteLine($"SignumTSGenerator finished with errors ({sw.ElapsedMilliseconds}ms)");
                foreach (var item in extra)
                    log.WriteLine($"{item.Value}:error STSG0002:t4s file not needed, Namespace {item.Key} does not export typescript types");
                return -1;
            }

            var missing = shouldT4s.Where(kvp => !currentT4s.ContainsKey(kvp.Key)).ToList();
            foreach (var m in missing)
            {
                var index = m.Key.IndexOf('.');

                var goodDirectory = index != -1 && Path.GetFileName(currentDirectory) == m.Key.Substring(0, index) ?
                    Path.Combine(currentDirectory, m.Key.Substring(index + 1).Replace('.', Path.DirectorySeparatorChar)) : null;

                var newT4S = Path.Combine(goodDirectory != null && Directory.Exists(goodDirectory) ? goodDirectory : currentDirectory, m.Key + ".t4s");

                currentT4Files.Add(newT4S);
                currentT4s.Add(m.Key, newT4S);
                log.WriteLine($"Automatically creating {newT4S}");
                File.WriteAllBytes(newT4S, new byte[0]);
            }

            foreach (var kvp in shouldT4s)
            {
                var t4sFile = currentT4s[kvp.Key];
                string result = EntityDeclarationGenerator.WriteNamespaceFile(options, t4sFile, kvp.Key, kvp.Value);

                var targetFile = Path.ChangeExtension(t4sFile, ".ts");
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

            {
                var upToDateContent = GetUpToDateContent(intermediateAssembly, currentT4Files);
                File.WriteAllText(signumUpToDatePath, upToDateContent);
            }
            log.WriteLine($"SignumTSGenerator finished ({sw.ElapsedMilliseconds}ms)");
            Console.WriteLine();
            return 0;

        }
        catch (Exception ex)
        {
            log.WriteLine($"SignumTSGenerator finished with errors ({sw.ElapsedMilliseconds}ms)");
            log.WriteLine($"{currentCsproj ?? "" }:error STSG0001:{ex.Message}");
            log.WriteLine(ex.StackTrace);
            return -1;
        }
    }

    private static List<string> GetProjectReferences(string currentCsproj)
    {
        var projXml = XDocument.Load(currentCsproj);

        return projXml.Document.Descendants("ProjectReference")
            .Select(a => a.Attribute("Include").Value.Replace('\\', Path.DirectorySeparatorChar))
            .ToList();
    }

    private static string GetUpToDateContent(string intermediateAssembly, List<string> t4Files)
    {
        return string.Join("\n", new[] { intermediateAssembly }.Concat(t4Files.OrderBy(a => a))
                     .Select(f => File.GetLastWriteTimeUtc(f).ToString("o") + " " + Path.GetFileName(f))
                    .ToList()
                );
    }

    private static bool ReferencesOrIsSignum(string csprojFilePath)
    {
        if (Path.GetFileName(csprojFilePath) == "Signum.csproj")
            return true;

        return GetProjectReferences(csprojFilePath).Any(csproj => Path.GetFileName(csproj) == "Signum.csproj");
    }

    public static List<string> GetAllT4SFiles(string reactDirectory)
    {
        var result = new List<string>();

        void Fill(string dir)
        {
            result.AddRange(Directory.EnumerateFiles(dir, "*.t4s"));

            foreach (var subDir in Directory.EnumerateDirectories(dir))
            {
                var subDirName = Path.GetFileName(subDir);
                if (subDirName == "obj" || subDirName == "bin" || subDirName == "node_modules" || subDirName == "ts_out")
                    continue;

                Fill(subDir);
            }
        }

        Fill(reactDirectory);

        return result;
    }
}
