
# CSharpRenderer
Utility class, useful for C# code-generation scenarios. 

```C#
public static class CSharpRenderer
{
    public static string GenerateCSharpCode(this Expression expression); //prints a C#-like string given an expression tree
    public static string GenerateCSharpCode(this Expression expression, string[] importedNamespaces);

    public static T Collapse<T>(this T obj); //Dummy method to collapse a collection or object initializer in one single line when using GenerateCSharpCode
    public static T Literal<T>(string literal); //Dummy method to allow introduce random strings in an expression tree when using GenerateCSharpCode

    //Methods that produce a more C#-ish representation of Members than the current ToString method.  
    public static string ParameterName(this ParameterInfo pi);
    public static string PropertyName(this PropertyInfo pi);
    public static string FieldName(this FieldInfo pi);
    public static string MethodName(this MethodInfo method);
    public static string MethodSignature(this MethodInfo method);
    public static string MemberName(this MemberInfo mi);
    public static string TypeName(this Type type);

    //Simplifies debugging of anonymous types and transparen identifiers (i.e.: let )
    public static string CleanIdentifiers(this string str)
    {
       return str
           .Replace("<>f__AnonymousType", "α") 
           .Replace("<>h__TransparentIdentifier", "τ");
    }

    //C#-ish representation of a simple object value (string litera, number, array, simple objects...)
    public static string Value(object valor, Type type, string[] importedNamespaces);
    public static CodeExpression GetRightExpressionForValue(object value, Type type, string[] importedNamespaces) //For CodeDom scenarios

    //Smart CodeTypeReference constructor that takes importedNamespaces into account
    public static CodeTypeReference TypeReference(Type type, string[] importedNamespaces)
}
```

