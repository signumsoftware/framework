using Mono.Cecil;
using System.Linq;
using Mono.Cecil.Cil;

namespace Signum.MSBuildTask;

internal class AutoPropertyConverter
{
    public AssemblyDefinition Assembly;
    public PreloadingAssemblyResolver Resolver;

    public AssemblyDefinition SigumEntities;
    public TypeDefinition ModifiableEntity;
    public MethodReference SetMethod;
    public MethodReference GetMethod;

    public AutoPropertyConverter(AssemblyDefinition assembly, PreloadingAssemblyResolver resolver)
    {
        this.Assembly = assembly;
        this.Resolver = resolver;

        this.SigumEntities = assembly.Name.Name == "Signum" ? assembly : resolver.Signum;
        this.ModifiableEntity = SigumEntities.MainModule.GetType("Signum.Entities", "ModifiableEntity");
        this.SetMethod = Assembly.MainModule.ImportReference(this.ModifiableEntity.Methods.Single(m => m.Name == "Set" && m.IsDefinition));
        this.GetMethod = Assembly.MainModule.ImportReference(this.ModifiableEntity.Methods.Single(m => m.Name == "Get" && m.IsDefinition));
    }

    internal bool FixProperties()
    {
        var entityTypes = (from t in this.Assembly.MainModule.Types
                           where IsModifiableEntity(t) && t.HasProperties
                           select t).ToList();

        foreach (var type in entityTypes)
        {
            FixProperties(type);
        }

        return false;
    }

    internal bool IsModifiableEntity(TypeDefinition t)
    {
        if (!t.IsClass)
            return false;

        if (!InheritsFromModEntity(t))
            return false;

        return true;
    }

    private bool InheritsFromModEntity(TypeDefinition t)
    {
        if (t.FullName == ModifiableEntity.FullName)
            return true;

        if (t.BaseType == null || t.BaseType.FullName == "System.Object")
            return false;

        var baseType = t.BaseType.Resolve();

        var result = InheritsFromModEntity(baseType);

        return result;
    }

    private void FixProperties(TypeDefinition type)
    {
        var fields = type.Fields.ToDictionary(a => a.Name);

        foreach (var prop in type.Properties.Where(p=>p.HasThis))
        {
            FieldDefinition field;
            if (fields.TryGetValue(BackingFieldName(prop), out field))
            {
                var targetField = type.GenericParameters.Count == 0 ? field :
                    field.MakeHostInstanceGeneric(type.GenericParameters.ToArray());

                if (prop.GetMethod != null)
                    ProcessGet(prop, targetField);

                if (prop.SetMethod != null)
                    ProcessSet(prop, targetField);
            }
        }
    }

    private void ProcessGet(PropertyDefinition prop, FieldReference field)
    {
        var inst = prop.GetMethod.Body.Instructions;
        prop.GetMethod.Body.Variables.Add(new VariableDefinition(field.FieldType));
        inst.Clear();
        inst.Add(Instruction.Create(OpCodes.Nop));
        inst.Add(Instruction.Create(OpCodes.Ldarg_0));
        inst.Add(Instruction.Create(OpCodes.Ldarg_0));
        inst.Add(Instruction.Create(OpCodes.Ldfld, field));
        inst.Add(Instruction.Create(OpCodes.Ldstr, prop.Name));
        inst.Add(Instruction.Create(OpCodes.Callvirt, this.GetMethod.MakeGenericMethod(prop.PropertyType)));
        inst.Add(Instruction.Create(OpCodes.Stloc_0));
        var loc = Instruction.Create(OpCodes.Ldloc_0);
        inst.Add(Instruction.Create(OpCodes.Br_S, loc));
        inst.Add(loc);
        inst.Add(Instruction.Create(OpCodes.Ret));
    }

    private void ProcessSet(PropertyDefinition prop, FieldReference field)
    {
        var inst = prop.SetMethod.Body.Instructions;
        inst.Clear();
        inst.Add(Instruction.Create(OpCodes.Nop));
        inst.Add(Instruction.Create(OpCodes.Ldarg_0));
        inst.Add(Instruction.Create(OpCodes.Ldarg_0));
        inst.Add(Instruction.Create(OpCodes.Ldflda, field));
        inst.Add(Instruction.Create(OpCodes.Ldarg_1));
        inst.Add(Instruction.Create(OpCodes.Ldstr, prop.Name));
        inst.Add(Instruction.Create(OpCodes.Callvirt, this.SetMethod.MakeGenericMethod(prop.PropertyType)));
        inst.Add(Instruction.Create(OpCodes.Pop));
        inst.Add(Instruction.Create(OpCodes.Ret));
    }



    static string BackingFieldName(PropertyDefinition p)
    {
        return "<" + p.Name + ">k__BackingField";
    }
}
