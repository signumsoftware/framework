using Mono.Cecil;
using System.Linq;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System.IO;

namespace Signum.MSBuildTask;

internal class FieldAutoInitializer
{
    public AssemblyDefinition Assembly;
    public PreloadingAssemblyResolver Resolver;
    public TextWriter Log;

    public TypeDefinition SystemType;
    public MethodDefinition GetTypeFromHandle;

    public AssemblyDefinition SigumEntities;
    public TypeDefinition AutoInit;
    public TypeDefinition OperationSymbol;
    public TypeDefinition OperationSymbolConstruct;

    public FieldAutoInitializer(AssemblyDefinition assembly, PreloadingAssemblyResolver resolver, TextWriter log)
    {
        this.Assembly = assembly;
        this.Resolver = resolver;
        this.Log = log;

        var module = resolver.SystemRuntime.MainModule;

        var tr = module.GetType("System", "Type");

        this.SystemType = tr.Resolve();
        this.GetTypeFromHandle = this.SystemType.GetMethods().Single(a => a.Name == "GetTypeFromHandle");

        this.SigumEntities = assembly.Name.Name == "Signum" ? assembly : resolver.Signum;
        this.AutoInit = SigumEntities.MainModule.GetType("Signum.Entities", "AutoInitAttribute");
        this.OperationSymbol = SigumEntities.MainModule.GetType("Signum.Operations", "OperationSymbol").Resolve();
        this.OperationSymbolConstruct = this.OperationSymbol.NestedTypes.Single(t => t.Name == "Construct`1").Resolve();

    }

    bool hasErrors = false;
    internal bool FixAutoInitializer()
    {
        var entityTypes = (from t in this.Assembly.MainModule.Types
                           where t.HasCustomAttributes && t.CustomAttributes.Any(a=>a.AttributeType.FullName == AutoInit.FullName)
                           select t).ToList();


        foreach (var type in entityTypes)
        {
            AutoInitFields(type);
        }
        return hasErrors;
    }

    private bool IsStatic(TypeDefinition t)
    {
        return t.IsClass && t.IsAbstract && t.IsSealed;
    }

    private void AutoInitFields(TypeDefinition type)
    {
        if (!IsStatic(type))
        {
            Log.WriteLine("Signum.MSBuildTask: {0} class should be static to use AutoInitAttribute", type.FullName);
            hasErrors = true;
            return;
        }

        if (type.Methods.Any(a => a.IsStatic && a.IsConstructor))
        {
            Log.WriteLine("Signum.MSBuildTask: {0} class should not have static constructor (or field initializers) to use AutoInitAttribute", type.FullName);
            hasErrors = true;
            return;
        }

        var newMethod = new MethodDefinition(".cctor",
            MethodAttributes.Private |
            MethodAttributes.HideBySig |
            MethodAttributes.SpecialName |
            MethodAttributes.RTSpecialName |
            MethodAttributes.Static,
            Assembly.MainModule.TypeSystem.Void);

        var inst = newMethod.Body.Instructions;

        foreach (var field in type.Fields)
        {
            if ((field.Attributes & FieldAttributes.InitOnly) == 0)
                field.Attributes |= FieldAttributes.InitOnly;

            var method = GetMethod(field.FieldType);
            if (method != null)
            {
                inst.Add(Instruction.Create(OpCodes.Ldtoken, type));
                inst.Add(Instruction.Create(OpCodes.Call, Assembly.MainModule.ImportReference(GetTypeFromHandle)));
                inst.Add(Instruction.Create(OpCodes.Ldstr, field.Name));

                if (method.Resolve().IsConstructor)
                    inst.Add(Instruction.Create(OpCodes.Newobj, Assembly.MainModule.ImportReference(method)));
                else
                    inst.Add(Instruction.Create(OpCodes.Call, Assembly.MainModule.ImportReference(method)));

                inst.Add(Instruction.Create(OpCodes.Stsfld, field));
            }
        }

        inst.Add(Instruction.Create(OpCodes.Ret));

        type.Methods.Add(newMethod);
    }

    private MethodReference GetMethod(TypeReference fieldType)
    {
        var generic = fieldType as GenericInstanceType;
        if (generic != null)
        {
            switch (generic.ElementType.FullName)
            {
                case "Signum.Operations.ConstructSymbol`1/Simple":
                    return OperationSymbolConstruct.Methods.Single(a => a.Name == "Simple")
                        .MakeHostInstanceGeneric(generic.GenericArguments.First());
                case "Signum.Operations.ConstructSymbol`1/From`1":
                    return OperationSymbolConstruct.Methods.Single(a => a.Name == "From")
                        .MakeHostInstanceGeneric(generic.GenericArguments.First())
                        .MakeGenericMethod(generic.GenericArguments.Last());
                case "Signum.Operations.ConstructSymbol`1/FromMany`1":
                    return OperationSymbolConstruct.Methods.Single(a => a.Name == "FromMany")
                        .MakeHostInstanceGeneric(generic.GenericArguments.First())
                        .MakeGenericMethod(generic.GenericArguments.Last());
                case "Signum.Operations.ExecuteSymbol`1":
                    return OperationSymbol.Methods.Single(a => a.Name == "Execute")
                       .MakeGenericMethod(generic.GenericArguments.Single());
                case "Signum.Operations.DeleteSymbol`1":
                    return OperationSymbol.Methods.Single(a => a.Name == "Delete")
                       .MakeGenericMethod(generic.GenericArguments.Single());
            }
        }


        var constructor = fieldType.Resolve().Methods.SingleOrDefault(a => a.IsConstructor && a.Parameters.Count == 2 &&
        a.Parameters[0].ParameterType.FullName == this.SystemType.FullName &&
        a.Parameters[1].ParameterType.FullName == Assembly.MainModule.TypeSystem.String.FullName);

        if (constructor == null)
        {
            Log.WriteLine("Signum.MSBuildTask: Type {0} has no constructor (Type, string)", fieldType.Name);
            return null;
        }

        return constructor;
    }
}
