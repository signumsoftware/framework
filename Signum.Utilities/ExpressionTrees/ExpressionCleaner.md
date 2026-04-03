# ExpressionCleaner

Expression cleaner is a complicated expression visitor that does many inter-related things:

1. Expand the expression using the three extensions methods described in **LinqExtensibility**
 * MethodExpanderAttribute
 * MemberXXXExpression static field
 * ExpressionExtensions.Expand method
2. Evaluate constant subexpressions
3. Simplify short-circuited expressions 

```C#
public class ExpressionCleaner : ExpressionVisitor
{
    public static Expression Clean(Expression expr)
    public static Expression Clean(Expression expr, Func<Expression, Expression> partialEval, bool shortCircuit)
}

```