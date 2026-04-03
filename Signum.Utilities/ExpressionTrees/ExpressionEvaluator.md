# ExpressionEvaluator

Expression Visitor that takes the nominated subtrees of the `ExpressionNominator`, compiles and evaluates them and then replaces each subtree by the corresponding value. 

`ExpressionEvaluator.Eval` also is able to evaluate a complete expression tree and return the value (or throw an exception if it's not possible to do so).  

```C#
public class ExpressionEvaluator : ExpressionVisitor
{
   public static Expression PartialEval(Expression exp);

   public static object Eval(Expression expression);
}
```

In order to avoid compilation of too many sub-expressions, the `ExpressionEvaluator` catches the compilations for common sub-expressions: 
* Instance Mebers
* Static Members
* Instance methods with no arguments.
* Static methods with no arguments. 
* Extension methods with no arguments. 