using System;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using System.Linq;
using Mono.Cecil.Cil;

namespace Signum.MSBuildTask
{
    internal class AutoExpressionField
    {
        private AssemblyDefinition Assembly;
        private TaskLoggingHelper Log;
        private PreloadingAssemblyResolver Resolver;
        private AssemblyDefinition SignumUtilities;
        public TypeDefinition ExpressionField;


        public AutoExpressionField(AssemblyDefinition assembly, PreloadingAssemblyResolver resolver, TaskLoggingHelper log)
        {
            this.Assembly = assembly;
            this.Resolver = resolver;
            this.Log = log;
            this.SignumUtilities = assembly.Name.Name == "Signum.Utilities" ? assembly : resolver.SignumUtilities;
            this.ExpressionField = SignumUtilities.MainModule.GetType("Signum.Utilities", "ExpressionFieldAttribute");
        }

        internal void FixAutoExpressionField()
        {
            var methodPairs = (from t in this.Assembly.MainModule.Types
                               where t.HasMethods
                               from m in t.Methods
                               where m.HasCustomAttributes
                               let at = m.CustomAttributes.SingleOrDefault(a => a.AttributeType.FullName == ExpressionField.FullName)
                               where at != null
                               where (string)at.ConstructorArguments.Single().Value == "auto"
                               select new { m, at }).ToList();

            var propertyPairs = (from t in this.Assembly.MainModule.Types
                                 where t.HasProperties
                                 from p in t.Properties
                                 where p.HasCustomAttributes
                                 let at = p.CustomAttributes.SingleOrDefault(a => a.AttributeType.FullName == ExpressionField.FullName)
                                 where at != null
                                 where (string)at.ConstructorArguments.Single().Value == "auto"
                                 select new { m = p.GetMethod, at }).ToList();


            foreach (var p in methodPairs.Concat(propertyPairs))
            {
                var fieldName = GetExpressionFieldName(p.m);
                if(fieldName != null)
                    p.at.ConstructorArguments[0] = new CustomAttributeArgument(p.at.ConstructorArguments[0].Type, fieldName);
            }
        }

        private string GetExpressionFieldName(MethodDefinition m)
        {
            var inst = m.Body.Instructions.Where(a => a.OpCode == OpCodes.Ldsfld);

            if(inst.Count() != 1)
            {
                LogError(m, inst.Count() + " Ldsfld found");
                return null;
            }

            var ld = inst.Single();

            var field = ((FieldReference)ld.Operand);

            if(!Same(field.DeclaringType, m.DeclaringType))
            {
                LogError(m, string.Format("field {0} is declared in a different class", field.Name));
                return null;
            }

            return field.Name;
        }

        private bool Same(TypeReference fieldDT, TypeReference memberDT)
        {
            if (fieldDT == memberDT)
                return true;

            if (fieldDT is GenericInstanceType gt)
                return gt.ElementType == memberDT;

            return true;
        }

        private void LogError(MethodDefinition m, string error)
        {
            Log.LogError("Signum.MSBuildTask: {0}.{1} should be a simple evaluation of an expression field in order to use ExpressionFieldAttribute without parameters ({2})",
                m.DeclaringType.Name, m.Name, error);
        }
    }
}