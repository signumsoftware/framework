using Mono.Cecil;
using Mono.Cecil.Rocks;
using System.Collections.Generic;

namespace Signum.MSBuildTask;

public static class MsBuildExtensions
{
    public static IEnumerable<TypeDefinition> Parents(this TypeDefinition initial)
    {
        for (var t = initial; t != null && !t.FullName.StartsWith("System."); t = t.BaseType?.Resolve())
            yield return t;
    }

    public static MethodReference MakeHostInstanceGeneric(this MethodReference self, params TypeReference[] args)
    {
        var reference = new MethodReference(
            self.Name,
            self.ReturnType,
            self.DeclaringType.MakeGenericInstanceType(args))
        {
            HasThis = self.HasThis,
            ExplicitThis = self.ExplicitThis,
            CallingConvention = self.CallingConvention
        };

        foreach (var parameter in self.Parameters)
        {
            reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
        }

        foreach (var genericParam in self.GenericParameters)
        {
            reference.GenericParameters.Add(new GenericParameter(genericParam.Name, reference));
        }

        return reference;
    }

    public static FieldReference MakeHostInstanceGeneric(this FieldReference self, params TypeReference[] args)
    {
        var reference = new FieldReference(
            self.Name,
            self.FieldType,
            self.DeclaringType.MakeGenericInstanceType(args))
        {
        };

        return reference;
    }

    public static GenericInstanceMethod MakeGenericMethod(this MethodReference reference, params TypeReference[] arguments)
    {
        var result = new GenericInstanceMethod(reference);
        foreach (var t in arguments)
            result.GenericArguments.Add(t);
        return result;
    }
}
