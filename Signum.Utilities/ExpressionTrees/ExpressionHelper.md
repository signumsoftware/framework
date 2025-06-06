
# ExpressionHelper 

Static helper class used by Expression Visitors.

```c#
public static class ExpressionHelper
{
    //Creates a new ReadOnlyCollection<T> if newValue returns a different value for some of them, otherwise the original
    public static ReadOnlyCollection<T> NewIfChange<T>(this ReadOnlyCollection<T> collection, Func<T,T> newValue)
    
    //Tries to convert to type, otherwise returns the original
    public static Expression TryConvert(this Expression expression, Type type)
 
    //Converts and expression of typo T to T?
    public static Expression Nullify(this Expression expression)
    
    //Get the expression associated with the parameter with name parameterName, otherwise throws IndexOutOfRangeException
    public static Expression GetArgument(this MethodCallExpression mce, string parameterName);
 
    //Get the expression associated with the parameter with name parameterName, otherwise returns null
    public static Expression TryGetArgument(this MethodCallExpression mce, string parameterName);

    //Removes all the Quote nodes (signs that a lambda should be considered an expression) 
    public static LambdaExpression StripQuotes(this Expression e); 

    //Returns true if a IQueryable Expression is pointing to itself (considered Table)
    public static bool IsBase(this IQueryable query); 
}
```
