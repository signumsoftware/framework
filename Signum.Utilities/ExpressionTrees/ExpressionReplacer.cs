
namespace Signum.Utilities.ExpressionTrees;

public class ExpressionReplacer: ExpressionVisitor
{
    Dictionary<ParameterExpression, Expression> replacements;

    public ExpressionReplacer(Dictionary<ParameterExpression, Expression> replacements)
    {
        this.replacements = replacements;
    }

    public static Expression Replace(InvocationExpression invocation)
    {
        LambdaExpression lambda = (LambdaExpression)invocation.Expression;
        var replacer = new ExpressionReplacer(0.To(lambda.Parameters.Count).ToDictionaryEx(i => lambda.Parameters[i], i => invocation.Arguments[i]));

        return replacer.Visit(lambda.Body);
    }

    public static Expression Replace(Expression expression, Dictionary<ParameterExpression, Expression> replacements)
    {
        var replacer = new ExpressionReplacer(replacements);

        return replacer.Visit(expression);
    }

    protected override Expression VisitParameter(ParameterExpression p)
    {
        return replacements.TryGetC(p) ?? p;
    }
}
