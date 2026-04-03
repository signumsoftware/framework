# QueryProvider

Implements all the 'scaffolding' so you can inherit an focus on the two important methods in order to implement your own Linq provider: `Execute` and `GetQueryText` (debugging support only). Your provider should inherit from this class.

Here's the full source code: 

```C#
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
       Type elementType = ReflectionTools.CollectionType(expression.Type) ?? expression.Type; 
       try
       {
            return (IQueryable)Activator.CreateInstance(typeof(Query<>).MakeGenericType(elementType), new object[] { this, expression });
       }
       catch (TargetInvocationException tie)
       {
           throw tie.InnerException;
       }
   }

   S IQueryProvider.Execute<S>(Expression expression)
   {
       return (S)this.Execute(expression);
   }

   object IQueryProvider.Execute(Expression expression)
   {
       return this.Execute(expression);
   }

   public abstract string GetQueryText(Expression expression);
   public abstract object Execute(Expression expression);
}
```