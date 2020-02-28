using Mono.Cecil;
using System.Linq;
using Mono.Cecil.Cil;
using System.IO;
using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using Mono.Cecil.Rocks;

namespace Signum.MSBuildTask
{
    internal class ExpressionFieldGenerator
    {
        AssemblyDefinition Assembly;
        TextWriter Log;
        PreloadingAssemblyResolver Resolver;
        AssemblyDefinition SystemRuntime;
        AssemblyDefinition SignumUtilities;
        TypeDefinition ExpressionField;
        TypeDefinition AutoExpressionField;
        TypeDefinition ExpressionExtensions;

        MethodReference Type_GetTypeFromHandle;
        MethodReference Expression_Parameter;
        MethodReference Expression_Lambda;
        MethodReference Array_Empty;
        TypeDefinition ParameterExpression;


        bool hasErrors = false;


        public ExpressionFieldGenerator(AssemblyDefinition assembly, PreloadingAssemblyResolver resolver, TextWriter log)
        {
            this.Assembly = assembly;
            this.Resolver = resolver;
            this.Log = log;
            this.SystemRuntime = resolver.SystemRuntime;
            this.SignumUtilities = assembly.Name.Name == "Signum.Utilities" ? assembly : resolver.SignumUtilities;
            this.ExpressionField = SignumUtilities.MainModule.GetType("Signum.Utilities", "ExpressionFieldAttribute");
            this.AutoExpressionField = SignumUtilities.MainModule.GetType("Signum.Utilities", "AutoExpressionFieldAttribute");
            this.ExpressionExtensions = SignumUtilities.MainModule.GetType("Signum.Utilities", "ExpressionExtensions");

            this.Type_GetTypeFromHandle = this.SystemRuntime.MainModule.GetType("System", "Type").Methods.Single(a => a.Name == "GetTypeFromHandle");
            var expressionType = resolver.SystemLinqExpressions.MainModule.GetType("System.Linq.Expressions", nameof(System.Linq.Expressions.Expression));
            this.Expression_Parameter = expressionType.Methods.Single(a => a.Name == nameof(Expression.Parameter) && a.Parameters.Count == 2);
            this.Expression_Lambda = expressionType.Methods.Single(a => a.Name == nameof(Expression.Lambda) && a.GenericParameters.Count == 1 && a.Parameters.Count == 2 && a.Parameters[1].ParameterType.IsArray);
            this.ParameterExpression = resolver.SystemLinqExpressions.MainModule.GetType("System.Linq.Expressions", nameof(System.Linq.Expressions.ParameterExpression));
            this.Array_Empty = resolver.SystemRuntime.MainModule.GetType("System", "Array").Methods.Single(a => a.Name == "Empty");
                
        }

        internal bool FixAutoExpressionField()
        {
            var methodPairs = (from type in this.Assembly.MainModule.Types
                               where type.HasMethods
                               from method in type.Methods
                               where method.HasCustomAttributes
                               let at = method.CustomAttributes.SingleOrDefault(a => a.AttributeType.FullName == AutoExpressionField.FullName)
                               where at != null
                               select new { type, method, member = (IMemberDefinition)method, at }).ToList();

            var propertyPairs = (from type in this.Assembly.MainModule.Types
                                 where type.HasProperties
                                 from p in type.Properties
                                 where p.HasCustomAttributes
                                 let at = p.CustomAttributes.SingleOrDefault(a => a.AttributeType.FullName == AutoExpressionField.FullName)
                                 where at != null
                                 select new { type, method = p.GetMethod, member = (IMemberDefinition)p, at }).ToList();

            var VoidType = this.SystemRuntime.MainModule.ImportReference(typeof(void));
            var StringType = this.SystemRuntime.MainModule.ImportReference(typeof(string));

            foreach (var p in methodPairs.Concat(propertyPairs))
            {
                string name = GetFreeName(p.member.Name, p.type);
                var fieldType = p.type.Module.ImportReference(GetExpressionType(p.method));
                var expressionField = new FieldDefinition(name + "Expression", FieldAttributes.Static | FieldAttributes.Private, fieldType);
                p.type.Fields.Add(expressionField);

                var newMethod = new MethodDefinition(name + "Init", MethodAttributes.Static | MethodAttributes.Private, VoidType);
                newMethod.IsHideBySig = true;
                var constructor = p.type.Methods.SingleOrDefault(a => a.IsConstructor && a.IsStatic);
                if (constructor == null)
                {
                    constructor = new MethodDefinition(".cctor", MethodAttributes.Private | MethodAttributes.Static  | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig, VoidType);
                    if (!constructor.IsConstructor)
                        throw new InvalidOperationException();

                    constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

                    p.type.Methods.Insert(0, constructor);
                }

                p.type.Methods.Add(newMethod);
                constructor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, newMethod));

                Transform(p.type.Module, p.method, newMethod, expressionField, out var closureType);

                if (closureType != null)
                    p.type.NestedTypes.Remove(closureType);

                p.member.CustomAttributes.Remove(p.at);
                var attrConstructor = p.type.Module.ImportReference(this.ExpressionField.Methods.Single(a => a.IsConstructor && a.IsPublic));
                p.member.CustomAttributes.Add(new CustomAttribute(attrConstructor)
                {
                    ConstructorArguments = { new CustomAttributeArgument(StringType, name + "Expression") }
                });
            }

            return hasErrors;
        }



        private void Transform(ModuleDefinition mod, MethodDefinition method, MethodDefinition newMethod, FieldDefinition expressionField, out TypeDefinition closureType)
        {
            var writer = newMethod.Body.GetILProcessor();
            Dictionary<ParameterReference, VariableDefinition> oldParameterToNNewVariable = new Dictionary<ParameterReference, VariableDefinition>();
            newMethod.Body.InitLocals = true;
            writer.Emit(OpCodes.Nop);
            var allParameters = method.Parameters.ToList();
            if (method.Body.ThisParameter != null)
                allParameters.Insert(0, method.Body.ThisParameter);

            foreach (var p in allParameters)
            {
                var variable = new VariableDefinition(mod.ImportReference(ParameterExpression));
                newMethod.Body.Variables.Add(variable);
                writer.Emit(OpCodes.Ldtoken, p.ParameterType);
                writer.Emit(OpCodes.Call, mod.ImportReference(this.Type_GetTypeFromHandle));
                writer.Emit(OpCodes.Ldstr, p == method.Body.ThisParameter ? "this" : p.Name);
                writer.Emit(OpCodes.Call, mod.ImportReference(this.Expression_Parameter));
                writer.Emit(OpCodes.Stloc, variable.Index);
                oldParameterToNNewVariable.Add(p, variable);
            }

            method.Body.SimplifyMacros();
            var reader = new ILReader(method.Body);
            //var c = new <>__DisplayClass
            bool isDup = false;
            Dictionary<MetadataToken, ParameterReference> captureFieldToParameter = new Dictionary<MetadataToken, ParameterReference>();
            closureType = null; 
            if (reader.Is(OpCodes.Newobj))
            {
                var newObj = reader.Get(OpCodes.Newobj);
                closureType = ((MethodDefinition)newObj.Operand).DeclaringType;
                if(reader.TryGet(OpCodes.Stloc) != null) //DEBUG: stloc(ldloc ldarg sfld) * ldloc
                {
                    //c.a = a;
                    while (LookaheadClosureAssignment(isDup, ref reader, out var oldParameter, out var fieldReference))
                    {
                        captureFieldToParameter.Add(fieldReference.MetadataToken, oldParameter);
                    }
                }
                else if (reader.Is(OpCodes.Dup)) //RELEASE: (dup ldarg stfld)*
                {
                    isDup = true;
                    while (LookaheadClosureAssignment(isDup, ref reader, out var oldParameter, out var fieldReference))
                    {
                        captureFieldToParameter.Add(fieldReference.MetadataToken, oldParameter);
                    }
                }
            }

            Dictionary<VariableDefinition, VariableDefinition> rebasedVariables = new Dictionary<VariableDefinition, VariableDefinition>();
            foreach (var v in method.Body.Variables)
            {
                if (closureType != null && v.VariableType == closureType)
                {

                }
                else
                {
                    var newVariable = new VariableDefinition(mod.ImportReference(v.VariableType));
                    rebasedVariables.Add(v, newVariable);
                    newMethod.Body.Variables.Add(newVariable);
                }
            }

            while (reader.HasMore())
            {
                if (LookaheadExpressionFieldConstant(isDup, ref reader, out var token))
                {
                    var param = captureFieldToParameter[token.Value];
                    writer.Emit(OpCodes.Ldloc, oldParameterToNNewVariable[param].Index);
                }
                else if (!method.IsStatic && LookaheadExpressionThisConstant(ref reader, method.DeclaringType))
                {
                    writer.Emit(OpCodes.Ldloc, oldParameterToNNewVariable[method.Body.ThisParameter].Index);
                }
                else if (LookaheadArray(ref reader))
                {
                    if (reader.HasMore())
                        throw new InvalidOperationException($"The method {method.FullName} should only call As.Expression with an expression tree lambda");

                    if (oldParameterToNNewVariable.Count == 0)
                    {
                        writer.Emit(OpCodes.Call, mod.ImportReference(new GenericInstanceMethod(this.Array_Empty) { GenericArguments = { this.ParameterExpression } }));
                    }
                    else
                    {
                        writer.Emit(OpCodes.Ldc_I4, oldParameterToNNewVariable.Count);
                        writer.Emit(OpCodes.Newarr, mod.ImportReference(this.ParameterExpression));
                        for (int i = 0; i < oldParameterToNNewVariable.Count; i++)
                        {
                            writer.Emit(OpCodes.Dup);
                            writer.Emit(OpCodes.Ldc_I4, i);
                            writer.Emit(OpCodes.Ldloc, i);
                            writer.Emit(OpCodes.Stelem_Ref);
                        }
                    }

                    var funcType = ((GenericInstanceType)expressionField.FieldType).GenericArguments.Single();
                    writer.Emit(OpCodes.Call, mod.ImportReference(new GenericInstanceMethod(this.Expression_Lambda) { GenericArguments = { funcType } }));
                    writer.Emit(OpCodes.Stsfld, expressionField);
                    writer.Emit(OpCodes.Ret);
                    writer.Body.OptimizeMacros();


                    {
                        method.Body.Instructions.Clear();
                        method.Body.Variables.Clear();
                        var oldWriter = method.Body.GetILProcessor();
                        oldWriter.Emit(OpCodes.Ldsfld, expressionField);
                        for (int i = 0; i < allParameters.Count; i++)
                            oldWriter.Emit(OpCodes.Ldarg, allParameters[i]);

                        var evaluate = ExpressionExtensions.Methods.Single(a => a.Name == "Evaluate" && a.GenericParameters.Count == allParameters.Count + 1);
                        var evaluateInstance = new GenericInstanceMethod(evaluate);
                        foreach (var p in allParameters)
                            evaluateInstance.GenericArguments.Add(p.ParameterType);
                        evaluateInstance.GenericArguments.Add(method.ReturnType);

                        oldWriter.Emit(OpCodes.Call, mod.ImportReference(evaluateInstance));
                        oldWriter.Emit(OpCodes.Ret);
                        oldWriter.Body.Optimize();

                    }
                    return;
                }
                else if (reader.TryGet(OpCodes.Ldloc) is Instruction ldloc)
                {
                    writer.Emit(ldloc.OpCode, rebasedVariables[(VariableDefinition)ldloc.Operand]);
                }
                else if (reader.TryGet(OpCodes.Stloc) is Instruction stloc)
                {
                    writer.Emit(stloc.OpCode, rebasedVariables[(VariableDefinition)stloc.Operand]);
                }
                else if (reader.TryGet(OpCodes.Ldloca) is Instruction ldloca)
                {
                    writer.Emit(ldloca.OpCode, rebasedVariables[(VariableDefinition)ldloca.Operand]);
                }
                else
                {
                    var ins = reader.Get();
                    writer.Append(ins);
                }
            }

            throw new InvalidOperationException($"The method {method.FullName} should only call As.Expression with an expression tree lambda");
        }

        bool LookaheadClosureAssignment(bool isDup, ref ILReader reader, out ParameterReference parameterReference, out FieldReference fieldReference)
        {
            fieldReference = null;
            parameterReference = null;
            var fork = reader.Clone();
            if ((isDup ? fork.TryGet(OpCodes.Dup) : fork.TryGet(OpCodes.Ldloc, 0)) != null &&
                fork.TryGet(OpCodes.Ldarg) is Instruction ldarg &&
                fork.TryGet(OpCodes.Stfld) is Instruction stfld)
            {
                parameterReference = (ParameterReference)ldarg.Operand;
                fieldReference = (FieldReference)stfld.Operand;
                reader.Position = fork.Position;
                return true;
            }
            return false;
        }

        bool LookaheadExpressionFieldConstant(bool isDup, ref ILReader reader, out MetadataToken? fieldToken)
        {
            fieldToken = null;
            var fork = reader.Clone();
            if (//Expression.Constant(typeof(<>__DisplayClass>), c)
                (isDup || fork.TryGet(OpCodes.Ldloc, 0 ) != null) &&
                fork.TryGet(OpCodes.Ldtoken) != null &&
                fork.TryGetCall(nameof(Type.GetTypeFromHandle)) != null &&
                fork.TryGetCall(nameof(Expression.Constant)) != null &&
                //Expression.Field( , fieldof(c.a));
                fork.TryGet(OpCodes.Ldtoken) is Instruction ldtoken &&
                fork.TryGetCall(nameof(System.Reflection.FieldInfo.GetFieldFromHandle)) != null &&
                fork.TryGetCall(nameof(Expression.Field)) != null)
            {
                fieldToken = ((FieldReference)ldtoken.Operand).MetadataToken;
                reader.Position = fork.Position;
                return true;
            }

            return false;
        }

        bool LookaheadExpressionThisConstant(ref ILReader reader, TypeReference thisType)
        {
            var fork = reader.Clone();
            if (//Expression.Constant(typeof(ThisType), this)
                fork.TryGet(OpCodes.Ldarg, 0) != null &&
                fork.TryGet(OpCodes.Ldtoken) is Instruction ldTok && ldTok.Operand.Equals(thisType) &
                fork.TryGetCall(nameof(Type.GetTypeFromHandle)) != null &&
                fork.TryGetCall(nameof(Expression.Constant)) != null)
            {
                reader.Position = fork.Position;
                return true;
            }

            return false;
        }

        bool LookaheadArray(ref ILReader reader)
        {
            var fork = reader.Clone();
            if (fork.TryGetCall(nameof(Array.Empty)) != null &&
                fork.TryGetCall(nameof(Expression.Lambda)) != null &&
                fork.TryGetCall("Expression") != null &&
                fork.TryGet(OpCodes.Ret) != null)
            {
                reader.Position = fork.Position;
                return true;
            }

            return false;
        }

        public struct ILReader
        {
            public int Position;
            public MethodBody Body;

            public ILReader Clone() => new ILReader(Body) { Position = Position };

            public ILReader(MethodBody body)
            {
                Body = body;
                this.Position = 0;
            }

            public Instruction Get()
            {
                return Body.Instructions[Position++];
            }

            public bool Is(OpCode opCode, int? operand = null)
            {
                var ins = Body.Instructions[Position];

                if (ins.OpCode != opCode)
                    return false;

                if (operand == null)
                    return true;

                if (ins.Operand is ParameterDefinition pd)
                    return pd.Sequence == operand;
                else if (ins.Operand is VariableDefinition vd)
                    return vd.Index == operand;
                else
                    throw new Exception("Not Expected " + ins.Operand.GetType());
            }

            public bool IsCall(string methodName)
            {
                var ins = Body.Instructions[Position];
                return ins.OpCode == OpCodes.Call && ins.Operand is MethodReference mr && mr.Name == methodName;
            }

            public Instruction TryGet(OpCode opCode, int? operand = null)
            {
                if (Is(opCode, operand))
                    return Get();

                return null;
            }

            public Instruction TryGetCall(string methodName)
            {
                if (IsCall(methodName))
                    return Get();

                return null;
            }

            public Instruction Get(OpCode opCode, int? operand = null)
            {
                if (Is(opCode, operand))
                    return Get();

                throw new InvalidOperationException($"{opCode} expected in Position {Position} of {Body.Method.FullName}");
            }

            public Instruction GetCall(string methodName)
            {
                if (IsCall(methodName))
                    return Get();

                throw new InvalidOperationException($"{OpCodes.Call} {methodName} expected in Position {Position} of {Body.Method.FullName}");
            }

            internal bool HasMore()
            {
                return this.Position < this.Body.Instructions.Count;
            }
        }

        private TypeReference GetExpressionType(MethodDefinition method)
        {
            var mod = method.DeclaringType.Module;

            var numParams = (method.Body.ThisParameter != null ? 1 : 0) + method.Parameters.Count + 1;
            var func = this.Resolver.SystemRuntime.MainModule.GetType("System", "Func`" + numParams);
            var funcGeneric = new GenericInstanceType(func);
            if (method.Body.ThisParameter != null)
                funcGeneric.GenericArguments.Add(method.DeclaringType);
            foreach (var p in method.Parameters)
                funcGeneric.GenericArguments.Add(p.ParameterType);
            funcGeneric.GenericArguments.Add(method.ReturnType);
            var exprType = this.Resolver.SystemLinqExpressions.MainModule.GetType("System.Linq.Expressions", "Expression`1");

            return new GenericInstanceType(exprType)
            {
                GenericArguments = { funcGeneric }
            };
        }

        private string GetFreeName(string name, TypeDefinition type)
        {
            for (int i = 0; ; i++)
            {
                var result = name + (i == 0 ? "" : i.ToString());
                if (!IsUsed(type, result + "Expression") && !IsUsed(type, result + "Init"))
                    return result;
            }
        }

        private bool IsUsed(TypeDefinition type, string v)
        {
            if (type.HasFields && type.Fields.Any(f => f.Name == v))
                return true;

            if (type.HasProperties && type.Properties.Any(f => f.Name == v))
                return true;

            if (type.HasMethods && type.Methods.Any(f => f.Name == v))
                return true;

            return false;
        }

        private void LogError(MethodDefinition m, string error)
        {
            Log.WriteLine("Signum.MSBuildTask: {0}.{1} should be a simple evaluation of an expression field in order to use ExpressionFieldAttribute without parameters ({2})", m.DeclaringType.Name, m.Name, error);
            hasErrors = true;
        }
    }
}
