using System;
using Mono.Cecil;
using System.Linq;
using System.Collections.Generic;
using Mono.Cecil.Cil;

namespace Signum.MSBuildTask
{
    internal class PropertyFixer
    {
        public AssemblyDefinition Assembly;
        public PreloadingAssemblyResolver Resolver;

        public AssemblyDefinition SigumEntities;
        public TypeDefinition ModifiableEntity;
        public MethodDefinition SetMethod;
        public MethodDefinition GetMethod;

        public PropertyFixer(AssemblyDefinition assembly, PreloadingAssemblyResolver resolver)
        {
            this.Assembly = assembly;
            this.Resolver = resolver;

            this.SigumEntities = assembly.Name.Name == "Signum.Entities" ? assembly : resolver.Resolve("Signum.Entities");
            this.ModifiableEntity = SigumEntities.MainModule.GetType("Signum.Entities", "ModifiableEntity");
            this.SetMethod = this.ModifiableEntity.Methods.Single(m => m.Name == "Set" && m.IsDefinition);
            this.GetMethod = this.ModifiableEntity.Methods.Single(m => m.Name == "Get" && m.IsDefinition);
        }

        internal void FixProperties()
        {
            var entityTypes = (from t in this.Assembly.MainModule.Types
                               where t.IsClass && Parents(t).Any(td => td.FullName == ModifiableEntity.FullName) && t.HasProperties
                               select t).ToList();

            foreach (var type in entityTypes)
            {
                FixProperties(type);
            }
        }

        private void FixProperties(TypeDefinition type)
        {
            var fields = type.Fields.ToDictionary(a => a.Name);

            foreach (var prop in type.Properties.Where(p=>p.HasThis))
            {
                FieldDefinition field;
                if (fields.TryGetValue(BackingFieldName(prop), out field))
                {
                    if (prop.GetMethod != null)
                        ProcessGet(prop, field);

                    if (prop.SetMethod != null)
                        ProcessSet(prop, field);
                }
            }
        }

        private void ProcessGet(PropertyDefinition prop, FieldReference field)
        {
            var inst = prop.GetMethod.Body.Instructions;
            inst.Clear();
            inst.Add(Instruction.Create(OpCodes.Nop));
            inst.Add(Instruction.Create(OpCodes.Ldarg_0));
            inst.Add(Instruction.Create(OpCodes.Ldarg_0));
            inst.Add(Instruction.Create(OpCodes.Ldfld, field));
            inst.Add(Instruction.Create(OpCodes.Ldstr, prop.Name));
            inst.Add(Instruction.Create(OpCodes.Callvirt, new GenericInstanceMethod(this.GetMethod) { GenericArguments = { prop.PropertyType } }));
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
            inst.Add(Instruction.Create(OpCodes.Callvirt, new GenericInstanceMethod(this.SetMethod) { GenericArguments = { prop.PropertyType } }));
            inst.Add(Instruction.Create(OpCodes.Pop));
            inst.Add(Instruction.Create(OpCodes.Ret));
        }

    

        static string BackingFieldName(PropertyDefinition p)
        {
            return "<" + p.Name + ">k__BackingField";
        }

        private static IEnumerable<TypeDefinition> Parents(TypeDefinition initial)
        {
            for (var t = initial; t != null && !t.FullName.StartsWith("System."); t = t.BaseType?.Resolve())
                yield return t;
        }
    }
}