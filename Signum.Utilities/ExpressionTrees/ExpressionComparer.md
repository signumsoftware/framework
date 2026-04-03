# ExpressionComparer

Expression Visitor that compares two expressions structurally for equality. 

```C#
public class ExpressionComparer
{
  public static bool AreEqual(Expression a, Expression b, 
	ScopedDictionary<ParameterExpression, ParameterExpression> parameterScope = null, 
	bool checkParameterNames = false)

    public static IEqualityComparer<E> GetComparer<E>(bool checkParameterNames) where E : Expression
}
```

When comparing two expressions, the ExpressionPerameters found are tricks: 

* Should the expression `a => a + 2` be equal to `b => b + 2`?  `checkParameterNames` regulates this behaviour. 


* Should the expression `a + 2` be equal to `a + 2` if `a` is not declared anywhere?  `parameterScope` let's you inform about this contextual parameters, if any. 

