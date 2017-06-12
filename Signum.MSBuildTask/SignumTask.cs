using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using Mono.Cecil.Pdb;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Signum.MSBuildTask
{
    public class SignumTask : Task
    {
        [Required]
        public string Assembly { get; set; }

        [Required]
        public string References { get; set; }
        
        public string KeyFile { get; set; }

        public override bool Execute()
        {
            Log.LogMessage("Signum.MSBuildTask starting: {0}", Assembly);
            try
            {
                var resolver = new PreloadingAssemblyResolver(References);
                bool hasPdb = File.Exists(Path.ChangeExtension(Assembly, ".pdb"));

                AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(Assembly, new ReaderParameters
                {
                    AssemblyResolver = resolver,
                    ReadingMode = ReadingMode.Deferred,
                    ReadSymbols = hasPdb,
                    InMemory = true,
                    SymbolReaderProvider = hasPdb ? new PdbReaderProvider() : null
                });

                if (AlreadyProcessed(assembly))
                {
                    Log.LogMessage("Signum.MSBuildTask already processed: {0}", Assembly);
                    return true;
                }

                new AutoPropertyConverter(assembly, resolver).FixProperties();
                new FieldAutoInitializer(assembly, resolver, Log).FixAutoInitializer();
                new AutoExpressionField(assembly, resolver, Log).FixAutoExpressionField();

                if (Log.HasLoggedErrors)
                    return true;

                MarkAsProcessed(assembly, resolver);

                assembly.Write(Assembly, new WriterParameters
                {
                    WriteSymbols = hasPdb,
                    SymbolWriterProvider = hasPdb ? new PdbWriterProvider() : null,
                    StrongNameKeyPair = KeyFile == null ? null : new StrongNameKeyPair(File.ReadAllBytes(KeyFile))
                });
                
                return true;
            }
            catch (Exception e)
            {
                Log.LogError("Exception in Signum.MSBuildTask: {0}", e.Message);
                return false;
            }
        }

        private bool AlreadyProcessed(AssemblyDefinition assembly)
        {
            var nameof = typeof(GeneratedCodeAttribute).FullName;
            var attr = assembly.CustomAttributes
                .Any(a => a.AttributeType.FullName == nameof && ((string)a.ConstructorArguments[0].Value) == "SignumTask");

            return attr;
        }

        private void MarkAsProcessed(AssemblyDefinition assembly, IAssemblyResolver resolver)
        {
            TypeDefinition generatedCodeAttribute = resolver.Resolve(AssemblyNameReference.Parse("System")).MainModule.GetType(typeof(GeneratedCodeAttribute).FullName);
            MethodDefinition constructor = generatedCodeAttribute.Methods.Single(a => a.IsConstructor && a.Parameters.Count == 2);

            TypeReference stringType = assembly.MainModule.TypeSystem.String;
            assembly.CustomAttributes.Add(new CustomAttribute(assembly.MainModule.ImportReference(constructor))
            {
                ConstructorArguments =
                {
                    new CustomAttributeArgument(stringType, "SignumTask"),
                    new CustomAttributeArgument(stringType, this.GetType().Assembly.GetName().Version.ToString()),
                }
            });
        }
    }
}
