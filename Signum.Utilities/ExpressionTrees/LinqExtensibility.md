
# LINQ Extensibility

Signum.Utilities contains a model to allow you expand **LINQ to Signum** provider, or any other `IQueryable` provider like **Linq to SQL** or **LINQ to Entities**. This way you can teach the Linq Provider to translate your own methods or properties to -presumably- SQL. 

The key concept is that you don't teach the provider to translate your C# members to SQL (this is a very complicated pipeline where it's not easy to get in) instead you teach them to translate your C# member to other C# expression that could be translate to SQL by the provider.

Our extensibility model has 3 ways to allow you expand your query provider. This way you can refactor and clean your Linq queries the same way you will if they where Linq to Objects. 

Many of the ideas and code of this extensibility model are integrated in Signum Utilities from the original version of [Tomáš Petříček](http://tomasp.net/blog/linq-expand.aspx)


## Model Nº1: static Expression

Imagine you have an entity like this

```C#
public class PersonEntity
{
    string county;
    public string Country
    {
       get { ... }
       set { ... }
    }

    public int IsAmerican
    { 
       get { return country ==  "USA"; }        
    }
}


(...)
Database.Query<PersonEntity>().Where(p=>p.IsAmerican)...; 
```

In this case, our Linq provider could use Country property (because is able to find country field), but the implementation of IsAmerican is **completely opaque for the provider** and if you use it in a DB query will complaint about it. 

In order to enable this property on queries you need to indicate the equivalent expression tree. You can easily do that like this: 

```C#
public class PersonEntity
{
    string country;
    public string Country
    {
       get { ... }
       set { ... }
    }

    static Expression<Func<PersonEntity,bool>> IsAmericanExpression = p=>p.Country == "USA"; 
    [ExpressionField] 
    public bool IsAmerican
    { 
       get { return IsAmericanExpression.Evaluate(this); }        
    }
}
```

Just by providing a static field with the same name of the member with `Expression` at the end, the expansion system will replace the query at runtime by something like this: 

```C#
Database.Query<PersonEntity>().Where(p=>p.Country == "USA")...; 
```

Some important things to mention: 
* Since the `IsAmerican` property is an instance property, we have to pass a `PersonEntity` as the first argument of our expression. If the property would be `static` you could save the `PersonEntity` parameter. 
* Is a good practice to keep the `static` expression `private`, so it won't clutter your IntelliSense. 
* As you see, we have also changed the implementation, and we are calling Evaluate... over an expression tree!!. When evaluated in memory, ExpresionExtensions.Evaluate extension method compiles, caches, and evaluates the expression. By doing this you don't need to replicate the definition of 'IsAmerican' twice, one in the member definition (IL) and other in the static expression.    

This also works for static / instance methods. Let's see an example, making IsAmerican an extension method defined in the Business Logic.

```C#
public static class PersonLogic
{
   public static bool IsAmerican(this PersonEntity person)
   {
       return person.Country == "USA";
   }
}
```

Could be replaced by

```C#
public static class PersonLogic
{
   static Expression<Func<PersonEntity,bool>> IsAmericanExpression = p => p.Country == "USA"; 
   [ExpressionField] 
   public static bool IsAmerican(this PersonEntity person)
   {
       return IsAmericanExpression.Invoke(person);
   }
}
```

Now it's an static extension method, so no need to pass `PersonLogic` as the first parameter. On the other side, you need to create the expression with the same number of parameters (out and ref not supported). In this case, the first parameter is a `PersonEntity`, coincidentally the expression is just the same that in the first example. 

**This technique is the simplest and the preferred one when the expression will be the same for any set of parameters.**
  

## Model Nº2: Evaluate over Expression Trees

Sometimes you just want to factor out some code locally in your queries (to use it more than once), or you have no control on the member's class and you have to conform with a 'twin' delegate. 

Example: 
```C#
public static void Start(SchemaBuilder sb) 
{
   (...)
   QueryLogic.Queries.Register(PeopleQueries.NotMarried, ()=>
     from p in Database.Query<PersonEntity>()
     where p.State == MaritalStatus.Single || p.State == MaritalStatus.Divorced
     select new 
     {
         Entity = p.ToLite(),
         p.Id,
         p.Name,
         p.Sex
     });

   QueryLogic.Queries.Register(PeopleQueries.NotMarriedAlive, ()=>
     from p in Database.Query<PersonEntity>()
     where (p.State == MaritalStatus.Single || p.State == MaritalStatus.Divorced) && p.Alive
     select new 
     {
         Entity = p.ToLite(),
         p.Id,
         p.Name,
         p.Sex
     });
}
```

As you see, the 'not married' predicate is redundant on the two queries, in order to avoid this redundancy you could just do: 

```C#
public static void Start(SchemaBuilder sb) 
{
   (...)

   Expression<Func<PersonEntity,bool>> notMarried = p => p.State == MaritalStatus.Single || p.State == MaritalStatus.Divorced;

     QueryLogic.Queries.Register(PeopleQueries.NotMarried, ()=>
       from p in Database.Query<PersonEntity>()
       where notMarried.Evaluate(p) 
       select new 
       {
           Entity = p.ToLite(),
           p.Id,
           p.Name,
           p.Sex
       });

     QueryLogic.Queries.Register(PeopleQueries.NotMarriedAlive, ()=>
        from p in Database.Query<PersonEntity>()
        where notMarried.Evaluate(p)  && p.Alive
        select new 
        {
            Entity = p.ToLite(),
            p.Id,
            p.Name,
            p.Sex
        });
}
```

`Evaluate`, when using inside of a `IQueryable` query, instead of Compile-Cache-Eval, it's applied [β-reduced](http://en.wikipedia.org/wiki/Lambda_calculus#.CE.B2-reduction) to the parameters of the query. 

In order to factor out the select expression we will need to do something like that: 

```C#
Expression<Func<PersonEntity, ¿?>> selector = p=> new 
     {
         Entity = p.ToLite(),
         p.Id,
         p.Name,
         p.Sex
     };
```

We need to tell the compiler about the `Expression` so it can create an expression tree, not a function.

Unfortunately, there's no way we can write an anonymous type... neither it's possible to partially infer a type in C# like this: 
  
```C#
Expression<Func<PersonEntity, var>> selector = (...)
```

> **Note:** Maybe if they whould have [choose 'auto' instead of 'var' as the keyword](http://stackoverflow.com/questions/1263527/better-word-for-inferring-variables-other-than-var-c/1263553#1263553) we could see this in some future versions, but with 'var'... I don't see this happening.

So, in order to do that, we need to do a compiler trick using the Linq static class, also from [Tomás](http://tomasp.net/blog/dynamic-linq-queries.aspx): 

```C#
public static class Linq
{
    //All the methods just return f.

    public static Expression<Func<R>> Expr<R>(Expression<Func<R>> f)
    public static Expression<Func<T, R>> Expr<T, R>(Expression<Func<T, R>> f)
    public static Expression<Func<T0, T1, R>> Expr<T0, T1, R>(Expression<Func<T0, T1, R>> f)
    public static Expression<Func<T0, T1, T2, R>> Expr<T0, T1, T2, R>(Expression<Func<T0, T1, T2, R>> f)
    public static Expression<Func<T0, T1, T2, T3, R>> Expr<T0, T1, T2, T3, R>(Expression<Func<T0, T1, T2, T3, R>> f)

    public static Func<T, R> Func<T, R>(Func<T, R> f)
    public static Func<T0, T1, R> Func<T0, T1, R>(Func<T0, T1, R> f)
    public static Func<T0, T1, T2, R> Func<T0, T1, T2, R>(Func<T0, T1, T2, R> f)
    public static Func<T0, T1, T2, T3, R> Func<T0, T1, T2, T3, R>(Func<T0, T1, T2, T3, R> f)
}
```

By calling `Linq.Expr` we tell the compiler that we want to generate an Expression, we set the parameter types explicitly but we rely on method type inference for the returning type. Clever!. 

So finally our code will be like this: 

```C#
public static void Start(SchemaBuilder sb) 
{
   (...)

   Expression<Func<PersonEntity,bool>> notMarried = p => p.State == MaritalStatus.Single || p.State == MaritalStatus.Divorced;
   var selector = Linq.Expr((PersonEntity p)=>new 
     {
         Entity = p.ToLite(),
         p.Id,
         p.Name,
         p.Sex
     });
 
   QueryLogic.Queries.Register(PeopleQueries.NotMarried, ()=> 
     from p in Database.Query<PersonEntity>()
     where notMarried.Evaluate(p)
     select selector.Evaluate(p)

   QueryLogic.Queries.Registerdqm(PeopleQueries.NotMarriedAlive, ()=> 
     from p in Database.Query<PersonEntity>()
     where notMarried.Evaluate(p) && p.Alive
     select selector.Evaluate(p)
}
```

**Note** This example illustrates the usage of Evaluate to expand your expression trees, but in this case we could make the code simpler avoiding query syntax like this: 

```C#
QueryLogic.Queries.Register(PeopleQueries.NotMarried, ()=>
    Database.Query<PersonEntity>() 
    .Where(notMarried)
    .Select(selector);

QueryLogic.Queries.Register(PeopleQueries.NotMarriedAlive, ()=>
    Database.Query<PersonEntity>()
    .Where(p=>notMarried.Evaluate(p) && p.Alive) //Here Evaluate is really necessary
    .Select(selector);
```
  

## Model Nº3: MethodExpander

Finally, the third and more advanced extensibility model is used when:

* A more dynamic approach is necessary *(i.e: You want a different expression tree depending of the parameters)*. 
* The simplicity of static expression is not enough *(i.e: You want different overloads to have different expression trees, or the method is generic)*. 

For example, the new 'Is' method, that returns true if two entities have the same Type and Id (even if they are different instances in memory), can be also used on queries. 

In order to do so we just need to compare (==) the two parameters, but since the method is generic we can't use the static Expression model. 

What we do is decorate the method with a MethodExpanderAttribute pointing to the class that will handle the expansion. 

```C#
[MethodExpander(typeof(IsExpander))]
public static bool Is<T>(this T entity1, T entity2)
    where T : class, IEntity
{
   (...)
}
```

Finally we create the class `IsExpander`, implementing `IMethodExpander.Expand` method.   

```C#
class IsExpander : IMethodExpander
{
    public Expression Expand(Expression instance, Expression[] arguments, Type[] typeArguments)
    {
        return Expression.Equal(arguments[0], arguments[1]);
    }
}
```

You are free to do whatever you want in the Expand method. Here we are creating the expression tree manually, but in the next example we see how you could also use a expression tree created by the compiler.

`IsInInterval` expression can't use static expression because it has different overloads (whether minDate and maxDate are null or not). 

```C#
[MethodExpander(typeof(IsInIntervalExpander1))]
public static bool IsInInterval(this DateTime date, DateTime minDate, DateTime maxDate)
{
    return minDate <= date && date < maxDate;
}

class IsInIntervalExpander1 : IMethodExpander
{
    static readonly Expression<Func<DateTime, DateTime, DateTime, bool>> func = (date, minDate, maxDate) => minDate <= date && date < maxDate;

    public Expression Expand(Expression instance, Expression[] arguments, Type[] typeArguments)
    {
        return Expression.Invoke(func, arguments[0], arguments[1], arguments[2]);
    }
}
```

By using Expression.Invoke (calling a delegate) we tell the expansion system that we want func to be applied, just like with static Expressions.

## ToExpandable
`ExpressionExtensions` contains some useful extensions over expression trees.

```C#
public static class ExpressionExtensions
{
    //Allows Extensibility over non-Signum IQueryable providers.  
    public static IQueryable<T> ToExpandable<T>(this IQueryable<T> q)
}
```

The last method is the most important one. Signum Framework has LINQ extensibility built-in, but other providers like Linq to SQL or Linq to Entities do not. By calling ToExpandable over the first table of the expression, you will be able to use the three extensibility models on other providers as well. [See more here](http://tomasp.net/blog/linq-expand.aspx).   

# Conclusion
Even if the explanation is a bit too long, the three extensibility options are quite simple. By using them you can clean and reduce your queries, teaching the provider how to translate your own business concepts to SQL queries and removing redundancy. 

As with every feature, use it with care. It's very easy to end up having a many layers of expansions, defining some business concepts in terms of others. Remembering that at the end all this will be translated to SQL will save you from huge SQL statements and performance problems.