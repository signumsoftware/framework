
namespace Signum.Utilities.ExpressionTrees;


/// <summary>
/// A basic abstract LINQ query provider
/// </summary>
public abstract class QueryProvider : IQueryProvider
{
    protected QueryProvider()
    {
    }

    IQueryable<S> IQueryProvider.CreateQuery<S>(Expression expression)
    {
        return new Query<S>(this, expression);
    }

    IQueryable IQueryProvider.CreateQuery(Expression expression)
    {
        Type elementType = expression.Type.ElementType() ?? expression.Type;
        try
        {
            return (IQueryable)Activator.CreateInstance(typeof(Query<>).MakeGenericType(elementType), new object[] { this, expression })!;
        }
        catch (TargetInvocationException e)
        {
            e.InnerException!.PreserveStackTrace();

            throw e.InnerException!;
        }
    }

    S IQueryProvider.Execute<S>(Expression expression)
    {
        return (S)this.Execute(expression)!;
    }

    object? IQueryProvider.Execute(Expression expression)
    {
        return this.Execute(expression);
    }

    public abstract string GetQueryText(Expression expression);
    public abstract object? Execute(Expression expression);
}
