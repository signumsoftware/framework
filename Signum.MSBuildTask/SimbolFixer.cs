using System;
using Mono.Cecil;
using System.Linq;
using System.Collections.Generic;
using Mono.Cecil.Cil;
using Microsoft.Build.Utilities;

namespace Signum.MSBuildTask
{
    internal class SymbolFixer
    {
        public AssemblyDefinition Assembly;
        public PreloadingAssemblyResolver Resolver;

        public AssemblyDefinition SigumEntities;
        public TypeDefinition SymbolEntity;
        public TypeDefinition SemiSymbolEntity;
        public TaskLoggingHelper Log;

        public SymbolFixer(AssemblyDefinition assembly, PreloadingAssemblyResolver resolver, TaskLoggingHelper log)
        {
            this.Assembly = assembly;
            this.Resolver = resolver;
            this.Log = log;

            this.SigumEntities = assembly.Name.Name == "Signum.Entities" ? assembly : resolver.Resolve("Signum.Entities");
            this.SemiSymbolEntity = SigumEntities.MainModule.GetType("Signum.Entities", "Symbol");
        }

        internal void FixProperties()
        {
            var entityTypes = (from t in this.Assembly.MainModule.Types
                               where IsStatic(t) && t.HasFields && t.Fields.Any(a=>a.)
                               select t).ToList();

            foreach (var type in entityTypes)
            {
                FixProperties(type);
            }
        }

        private bool IsStatic(TypeDefinition t)
        {
            return t.IsClass && t.IsAbstract && t.IsSealed;
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
            //inst.Add(Instruction.Create(OpCodes.Callvirt, new GenericInstanceMethod(this.GetMethod) { GenericArguments = { prop.PropertyType } }));
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
            //inst.Add(Instruction.Create(OpCodes.Callvirt, new GenericInstanceMethod(this.SetMethod) { GenericArguments = { prop.PropertyType } }));
            inst.Add(Instruction.Create(OpCodes.Pop));
            inst.Add(Instruction.Create(OpCodes.Ret));
        }
    }
}