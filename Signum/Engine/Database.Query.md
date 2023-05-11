# Database.Query

Take a comfortable seat, breath deeply and prepare yourself. `Database.Query<T>()` is Signum Engine's gate to the LINQ world, a world of elegant compiled-time checked and IntelliSense assisted queries that will empower you to a new level of productivity. 

By taking ideas from functional languages (like lambdas and expression trees) with the very practical purpose of integrating queries in the language, Microsoft has completely changed the way enterprise code has to be done. Anyone who has used LINQ in some of its' flavors just won't turn back.

# A bit of context
The design that Anders Hejlsberg's team did with LINQ was really neat. The whole LINQ thing is just a bunch of general purpose additions to the language, they didn't hard-code Sql Server code in the C# compiler. Instead they keep the door open to add LINQ Providers by implementing the `IQueryable` interface. 

They created two Sql providers. [Linq to Sql](http://msdn.microsoft.com/en-us/library/bb425822.aspx) and [Linq to Entities](http://msdn.microsoft.com/en-us/library/bb386964.aspx), both encouraging a database-first approach.

The number of third-party LINQ providers is growing slowly, not because is not perceived as a useful feature, but because the technical challenge that it supposes. 

In Spring'08 we tried to use our ORM with Microsoft Linq to Sql, but our engine's philosophy didn't play well. We had each entity duplicated so half of our business logic was translating Signum Entities to LINQ proxy classes back an forth. Also, as we usually do applications with a lot of entities, we ended up with huge dbml files hard to maintain. Then we took the hard way, building our own Linq provider that speaks straight in terms of our entities. 

We're proud to have a full LINQ provider since Mid 2008, and we have invested lots of time and resources in improving it since them: 

* **Mid 2008:** Initial LINQ implementation, already with all the operators including `GroupBy` etc...
* **Late 2008:** Support of polymorphic foreign keys (cast, `is` and `as`)
* **Mid 2009:** The `Retriever` is deprecated and the LINQ provider is also responsible for retrieving entities.
* **Late 2008:** Polymorphism using Switch.
* **Early 2010:** Correlated sub-queries to avoid N+1 problem.
* **Early 2010:** Support of `Sytem.Type`.
* **Late 2011:** `UnsafeUpdate` and `UnsafeDelete`.
* **Mid 2012:** The `Deleter` is deprecated and the LINQ provider is responsible for deleting entities.
* **Late 2012:** Better support of polymorphism inheritance using Union or Switch support.
* **Early 2012:** Mixin support.
* **Late 2013:** `UnsafeInsert`.



## Getting started with LINQ
**LINQ to Signum** tries to implement LINQ Standard Query Operators in a straightforward way, but stills has some peculiarities.

Here we are going to focus on these peculiarities, because there are plenty of sites already where you can get an explanation of what LINQ is and how the Standard Query Operators work. Just to enumerate some of them: 

* [LINQ Standard Query Operators](http://aspnetresources.com/downloads/linq_standard_query_operators.pdf): A nice 2 pages abstract with all the Linq operators.
* [The LINQ project](http://msdn.microsoft.com/en-us/library/vstudio/bb397926.aspx): Main page of Linq project.
* [101 LINQ Samples](http://code.msdn.microsoft.com/101-LINQ-Samples-3fb9811b): A collection of Linq queries explained.
* [Linq at Wikipedia](http://en.wikipedia.org/wiki/Language_Integrated_Query): Wikipedia has always a good introduction about anything :)
* [Hooked on Linq](http://www.hookedonlinq.com/): A nice Wiki focused on Linq.
* [LINQPad](http://www.linqpad.net/): Really cool tool for writing Linq to Sql queries having a responsive and interactive experience.

So, at this point, you should be a proficient Linq programmer with experience in some of the non-Signum LINQ flavors, and you're eager to know how our provider works. 

Not so fast, first it would be convenient to take a look and get used to the data model we are going to query against in the examples.

## `Query<T>`

The first thing to notice is that we didn't generate any `db.Customer` property for each table, instead you have to write `Database.Query<CustomerEntity>()` to get the `IQueryable<CustomerEntity>` to start querying `CustomerEntity` table. You can find out why in [Database page](Database.md). 

```C#
public static IQueryable<T> Query<T>() where T : Entity
```


Let's see our first example, if you write a query like this:

```C#
 var result = from b in Database.Query<BugEntity>()
              group b by b.Status into g
              select new { Status = g.Key, Num = g.Count() };
```

It will be translated to this SQL:

```SQL
SELECT s2.agg1 AS c0, s2.idStatus
FROM (
  (SELECT bdn.idStatus, COUNT(*) AS agg1
  FROM BugEntity AS bdn
  GROUP BY bdn.idStatus)
) AS s2
```

### `e.InDB`

It's common that you want to start a query from a `Lite<T>` or entity: 

```C#
Lite<BugEntity> bug = //...
Databas.Query<BugEntity>().Where(b => b.ToLite().Is(bug)).Select(b => b.Comments.Count).SingleEx();
```

The following pattern can be simplified using `InDB` method:

```C#
public static IQueryable<E> InDB<E>(this E entity)  where E : class, IEntity
public static IQueryable<E> InDB<E>(this Lite<E> lite) where E : class, IEntity
```

This overload already does the `Database.Query` and the `Where` for you. Result:

```C#
Lite<BugEntity> bug = //...
bug.InDB().Select(b => b.Comments.Count).SingleEx();
```

### `e.InDB(selector)`

Even more, the pattern can be simplified further using `InDB(selector)` method!. Take a look: 

```C#
public static R InDB<E, R>(this Lite<E> lite, Expression<Func<E, R>> selector) where E : class, IEntity

public static R InDBEntity<E, R>(this E entity, Expression<Func<E, R>> selector) where E : class, IEntity
```

This overload (in addition to `Database.Query` and the `Where`), already does the `Select` and `SingleEx`. Result:


```C#
Lite<BugEntity> bug = //...
bug.InDB(b => b.Comments.Count); //That simple!
```

One important things to note is that if `InDB` is used **inside** of a query... just disappears! That let you write expression like: 

```C#
static Expression<Func<BugEntity, int>> CommentCountExpression =
    entity => entity.InDBEntity(e => e.Comments.Count); //Works in-memory and in-database
public static int CommentCount(this BugEntity entity)
{
    return CommentCountExpression.Evaluate(entity); 
}
```

> **Note:** Avoid using `InDB(selector)` to return collections. The current implementation has problem with the type-system and and the query translation. 

## `MListQuery(mListProperty)` (Advanced)

Most of the tables in the database have a 1-to-1 relationship with each entity type, but there is also a second type of tables, the ones that have 1-to-1 relationship with a `MList<T>` property of an entity type. We call this tables **MListTable**.

While this tables are secondary citizens, and are usually manipulated implicitly when saving changes in the main entity, there's a way to query them directly without having to do a select many.

So you can access the elements in a collection using `SelectMany`

```C#
Database.Query<BugEntity>().SelectMany(a => a.Comments).Select(a=>a.Date)
```

And will be translated to

```C#
SELECT s1.Date
FROM BugEntity AS bdn
CROSS APPLY (
  (SELECT bdnc.Date
  FROM BugDNComments AS bdnc
  WHERE (bdn.Id = bdnc.idParent))
) AS s1
```

But if you want direct access to the `BugDNComments` table, you can use `MListQuery` like this:


```C#
Database.MListQuery((BugEntity bug) => bug.Comments).Select(mle => mle.Element.Date)
```

That gets translated to the simpler SQL:

```SQL
SELECT bdnc.Date
FROM BugDNComments AS bdnc
```

Note how the `MListQuery<E, V>` takes a simple expression to the `mListProperty`, no fancy stuff here!:

```C#
public static IQueryable<MListElement<E, V>> MListQuery<E, V>(Expression<Func<E, MList<V>>> mListProperty)
   where E : Entity
```

And it returns a  `IQueryable<MListElement<E, V>>`, defined like: 

```C#
public class MListElement<E, V> where E : Entity
{
    public int RowId { get; set; }
    public int Order { get; set; }
    public E Parent { get; set; }
    public V Element { get; set; }
}
```
`MListElement<E, V>` object let's you have low-level access to all the columns in a MListTable that are usually managed implicitly: 

* **RowId**: The Primary Key of the MListTable. Accessible in-memory using `MList<T>.RowIdValue.RowId`.
* **Order**: The Order of the MListTable to save/restore the order of `MList<T>` properties with `PreserveOrderAttribute`. Accessible in-memory using `MList<T>.RowIdValue.OldIndex` (old) and just `IndexOf` (live).
* **Parent**: Reference to the parent entity. 
* **Element**: The element itself of the collection, could be a value, an `Entity`, an `EmbeddedEntity`, a `Lite<T>`, ...

`MListElement<E, V>` are necessary to manupulate the MListTable using [UnsafeDelete](Database.UnsafeDelete.md), [UnsafeUpdate](Database.UnsafeUpdate.md) or [UnsafeInsert](Database.UnsafeInsert.md). 

### `e.MListElements(mListProperty)`

Finishing the circle, `MListElements(mListProperty)` method is to `MListQuery<T>`, what `e.InDB` is to `Query<T>`. 

Sometimes you need to get the `MListElement<E, V>` of a particular entity. Instead of writing: 

```C#
BugEntity bug;
Database.MListQuery((BugEntity b) => b.Comments).Where(mle => mle.Parent.Is(bug));
```

You can use `MListElements(mListProperty)` defined as:

```C#
public static IQueryable<MListElement<E, V>> MListElements<E, V>(this E entity, Expression<Func<E, MList<V>>> mListProperty)
     where E : Entity

public static IQueryable<MListElement<E, V>> MListElementsLite<E, V>(this Lite<E> entity, Expression<Func<E, MList<V>>> mListProperty)
    where E : Entity
```

To write just: 

```C#
BugEntity bug;
bug.MListElement(b => b.Comments);
```

`MListElements(mListProperty)` also works inside of a query. 


## Where the code gets executed

In any LINQ query based in expression trees, is up to the provider to decide what and how will be executed, like what parts of the query will be executed in C# or SQL. Usually is a mixture of both: 

* All the constant sub-expressions are evaluated in C# and passed to SQL as a simple `SqlParameter` (avoiding SQL injection).
* If the expression are part of a `WHERE` condition, a `GROUP BY`, etc..., then it will be completely translated to SQL. If not possible an exception is thrown.
* But, if the expression is going to be evaluated *at the end* of the query (last selector), it will be partially executed in C# and SQL.
   
So if you define a local function that is not known (hard coded) by Linq to Signum, or makes no use of the  [LINQ Extensibility options](../Signum.Utilities/ExpressionTrees/LinqExtensibility.md): 

```C#
public static string ToPascalCase(string text)
{
    //(...)
}
```

You can still write something like this: 

```C#
var result = from b in Database.Query<BugEntity>()
             select ToPascalCase(b.Description);
```

And will be translated to

```SQL
SELECT bdn.Description FROM BugEntity AS bdn 
```

Executing your function for each row as they come from the database before returning the results to the client code. 

If you try something like this, however: 

```C#
var result = from b in Database.Query<BugEntity>()
             where ToPascalCase(b.Description) == "Hi"
             select b.Description;
```

It throws `InvalidOperationException("The expression can not be translated to SQL: ...")` because there's no possible way for SQL to know what your function does. 

### InSql

In the last select, you can force a sub-expression to be evaluated in SQL using `InSql` extension method, defined in `LinqHints`. 

For example: 

```C#
Database.Query<BugEntity>().Select(a => a.Id + a.Project.Id)
```

Just translates just to:

```SQL
SELECT bdn.Id, bdn.idProject
FROM BugEntity AS bdn
```

So the `+` operation is done in-memory as the results come from the database.

But if we write this:

```C#
Database.Query<BugEntity>().Select(a => (a.Id + a.Project.Id).InSql());
```

Will translate instead to

```SQL
SELECT (bdn.Id + bdn.idProject) AS c0
FROM BugEntity AS bdn
```

Now the operation is done in-database. 














