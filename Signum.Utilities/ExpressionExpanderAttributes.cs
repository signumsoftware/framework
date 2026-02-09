
namespace Signum.Utilities;

/// <summary>
/// Interface for classes that can be used to convert calls to methods
/// in LINQ expression trees.
/// </summary>
public interface IMethodExpander
{
    Expression Expand(Expression? instance, Expression[] arguments, MethodInfo mi);
}


public class GenericMethodExpander
{
    public LambdaExpression GenericLambdaExpression;
    public GenericMethodExpander(LambdaExpression genericLambdaExpression)
    {
        this.GenericLambdaExpression = genericLambdaExpression;
    } 
}

/// <summary>
/// Attribute to define the class that should be used to convert calls to methods
/// in LINQ expression trees
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class MethodExpanderAttribute : Attribute
{
    private Type expanderType;
    public Type ExpanderType
    {
        get { return expanderType; }
    }

    /// <param name="type">A class that implements IMethodExpander</param>
	public MethodExpanderAttribute(Type type)
    {
        expanderType = type;
    }
}

[System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public class ForceEagerEvaluationAttribute : Attribute
{
  
}

[System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public class AvoidEagerEvaluationAttribute : Attribute
{

}

[System.AttributeUsage(AttributeTargets.Constructor, Inherited = false, AllowMultiple = true)]
public class NewCanBeConstantAttribute : Attribute
{

}

//The member is polymorphic and should be expanded in a latter stage
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
public sealed class PolymorphicExpansionAttribute : Attribute
{
    public PolymorphicExpansionAttribute()
    {
    }
}

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class EagerBindingAttribute : Attribute
{

}

/// <summary>
/// Associates a method or property with a static field of type Expression with an equivalent definition that can be used inside IQueryable queries
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class ExpressionFieldAttribute : Attribute
{
    public string Name { get; set; }
    /// <param name="name">The name of the field for the expression that defines the content. If not set, will be automatically found from the method body.</param>
    public ExpressionFieldAttribute(string name)
    {
        this.Name = name;
    }
}

/// <summary>
/// Marks a property or method for Signum.MSBuildTask to extract the body into and static field with the expression tree. 
/// </summary>
[System.AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
public sealed class AutoExpressionFieldAttribute : Attribute
{
}

public static class As
{
    /// <summary>
    /// In Combination with AutoExpressionFieldAttribute, allows the extraction of 'body' expression into an static field (by Signum.MSBuildTask) so the method can be consumed by the LINQ provider and translated to SQL.
    /// </summary>
    /// <typeparam name="T">return type</typeparam>
    /// <param name="body">The implementation of the property or method</param>
    /// <returns></returns>
    public static T Expression<T>(Expression<Func<T>> body)
    {
        throw new InvalidOperationException("""
            This method is not meant to be called!!
            Did you forget the AutoExpressionFieldAttribute or is the project missing reference to Signum.MSBuildTask in this assembly?
            """);
            
    }

    public static LambdaExpression GetExpressionUntyped(MemberInfo methodOrProperty)
    {
        var attr = methodOrProperty.GetCustomAttribute<ExpressionFieldAttribute>();

        if (attr == null)
            throw new InvalidOperationException($"The member {methodOrProperty.Name} has not {nameof(ExpressionFieldAttribute)}");

        var fi = methodOrProperty.DeclaringType!.GetField(attr.Name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;

        return (LambdaExpression)fi.GetValue(null)!;
    }

    public static Expression<Func<T, R>> GetExpression<T, R>(Expression<Func<T, R>> methodOrProperty)
    {
        MemberInfo member = GetMember(methodOrProperty);

        return (Expression<Func<T, R>>)GetExpressionUntyped(member);
    }

    private static MemberInfo GetMember(LambdaExpression methodOrProperty)
    {
        var body = methodOrProperty.Body;

        if (body is UnaryExpression u && u.NodeType == ExpressionType.Convert)
            body = u.Operand;

        var member = body is MemberExpression m ? m.Expression!.Type.GetProperty(((PropertyInfo)m.Member).Name) ?? m.Member :
            body is MethodCallExpression mc ? mc.Object?.Type.GetMethod(mc.Method.Name, mc.Method.GetParameters().Select(p => p.ParameterType).ToArray()) ?? mc.Method :
            throw new InvalidOperationException($"Unexpected expression of type {body.NodeType}");
        return member;
    }

    public static void ReplaceExpressionUntyped(MemberInfo methodOrProperty, LambdaExpression newExpression)
    {
        var attr = methodOrProperty.GetCustomAttribute<ExpressionFieldAttribute>();

        if (attr == null)
            throw new InvalidOperationException($"The member {methodOrProperty.Name} has not {nameof(ExpressionFieldAttribute)}");

        var fi = methodOrProperty.DeclaringType!.GetField(attr.Name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;

        fi.SetValue(null, newExpression);
    }

    public static void ReplaceExpression<T, R>(Expression<Func<T, R>> methodOrProperty, Expression<Func<T, R>> newExpression)
    {
        MemberInfo member = GetMember(methodOrProperty);

        ReplaceExpressionUntyped(member, newExpression);
    }
}
