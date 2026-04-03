
# ExpressionNominator
Expression Visitor is able to detect constant subtrees of an expression tree that could potentially be replaced by just a value. This capability is what allows that IQueryable Linq queries blur the boundaries between your code and, presumably, SQL. 

```c#
public class ExpressionNominator : ExpressionVisitor
{
   public static HashSet<Expression> Nominate(Expression expression)
}

```