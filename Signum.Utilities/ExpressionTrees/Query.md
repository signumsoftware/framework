# Query<T>

Basic implementation of `IQueryable<T>`. Following Microsoft guidelines, relies in `QueryProvider` for most of the functionality. You should use the class as-is, without inheriting. 

```C#
public class Query<T> : IQueryable<T>, IQueryable, IEnumerable<T>, IEnumerable, IOrderedQueryable<T>, IOrderedQueryable
{
    QueryProvider provider;
    Expression expression;

    public Query(QueryProvider provider)
    public Query(QueryProvider provider, Expression expression)

    public Expression Expression {get;}
    public Type ElementType {get;}
    public IQueryProvider Provider {get;}
    public IEnumerator<T> GetEnumerator() {get;}
    IEnumerator IEnumerable.GetEnumerator() {get;}
    public override string ToString();
    public string QueryText{get;}
}
```