# LINQ Inheritance

## Inheritance support
Signum Framework uses polymorphic foreign key to implement [inheritance](../Signum.Entities/Inheritance.md), and LINQ to Signum supports all the expected operations naturally: 

### `is` operator

Is operator let you test if object instance *is* of a certain type. You can also use it in queries: 

```C#
var result = from b in Database.Query<BugEntity>()
             where b.Discoverer is DeveloperEntity
             select b.Description;
```

Translates to

```SQL
SELECT bdn.Description
FROM BugEntity AS bdn
WHERE (bdn.idDiscoverer_Developer) IS NOT NULL
```

Note: Don't confuse C# `is` operator (like in `fruit is OrangeEntity`) with `.Is()` extension method to compare entities (like `person.is(john)`). 

### Casting

C# has two ways of doing casting: Typical casting using type between parentheses `(DeveloperEntity)`, and the `as` operator. In case of an invalid conversion, casting will throw an `InvalidCastException` while `as` returns `null`.

At the database level makes no sense to throw an `InvalidCastException` for some particular row, so both are implemented similar to the `as` operator. 

So, this two queries are similar:
 
```C#
var result = from b in Database.Query<BugEntity>()
             select ((DeveloperEntity)b.Discoverer).Name;

var result = from b in Database.Query<BugEntity>()
             select (b.Discoverer as DeveloperEntity).Name; 
````

Additionally, since the implicit joins are implemented as `LEFT OUTER JOINS`, the effect is that nulls propagate without throwing `NullReferenceException`, so the result, if in-memory will be something like: 

```C#
var result = from b in Database.Query<BugEntity>()
             select b.Discoverer is DeveloperEntity ? null : ((DeveloperEntity)b.Discoverer).Name; 
````

Or using the new [null-propagating operator `?.`](https://roslyn.codeplex.com/discussions/540883).

```C#
var result = from b in Database.Query<BugEntity>()
             select (b.Discoverer as DeveloperEntity)?.Name; 
```

++Note:** Unfortunately `?.` was not yet been implemented in C# for expression trees. 

```SQL
SELECT bdn.Description
FROM BugEntity AS bdn
WHERE (bdn.idDiscoverer_Developer) IS NOT NULL
```


### GetType

Using `GetType` method and comparing or returning `System.Type` objects is also fully supported. For example:

```C#
var result = from b in Database.Query<BugEntity>()
             where b.Discoverer.GetType() == typeof(DeveloperEntity) 
             select b.Project.EntityType;
```

Is translated to:

```SQL
SELECT bdn.idProject
FROM BugEntity AS bdn
WHERE (bdn.idDiscoverer_Developer) IS NOT NULL
```

And returns a `List<System.Type>`, with all the elements being `typeof(ProjectEntity)`. 


### Polymorphism

Signum Framework also supports polymorphism for `ImplementedBy` expressions, so will be able to translate this query even if `Name` property is declared in `IBugDiscoverer` interface. 

```C#
var result = from b in Database.Query<BugEntity>()
             where b.Discoverer.Name == "John"
             select b.Description;
```

The translations joins with the two implementations, and uses a `CASE` expression to combine the results. 

```SQL
SELECT bdn.Description
FROM BugEntity AS bdn
LEFT OUTER JOIN CustomerEntity AS cdn
  ON (bdn.idDiscoverer_Customer = cdn.Id)
LEFT OUTER JOIN DeveloperEntity AS ddn
  ON (bdn.idDiscoverer_Developer = ddn.Id)
WHERE (
  CASE
    WHEN (bdn.idDiscoverer_Customer) IS NOT NULL THEN cdn.Name
    WHEN (bdn.idDiscoverer_Developer) IS NOT NULL THEN ddn.Name
    END
   = 'John')
```

Unfortunately, in our experience this type of complex conditions, using `OR` or `CASE` expresison in SQL Server, tend to be slow for big sets of data. So **don't abuse them!**

Sometimes the query can get faster using a `UNION` strategy instead of a `CASE`, but we haven't found an heuristic to make the choice automatic. In the meantime, you can use `CombineUnion` extension method over any `ImplementedBy` property to change the behavior. 

```C#
var result = from b in Database.Query<BugEntity>()
             where b.Discoverer.CombineUnion().Name == "John"
             select b.Description;
```

Translates instead to: 

```SQL
SELECT bdn.Description
FROM BugEntity AS bdn
LEFT OUTER JOIN (
    (SELECT cdn.Id AS Id_Customer, NULL AS Id_Developer, cdn.Name
    FROM CustomerEntity AS cdn)
  UNION ALL
    (SELECT NULL AS Id_Customer, ddn.Id AS Id_Developer, ddn.Name
    FROM DeveloperEntity AS ddn)
  ) AS uibd
  ON (((uibd.Id_Customer = bdn.idDiscoverer_Customer) OR ((uibd.Id_Customer) IS NULL AND (bdn.idDiscoverer_Customer) IS NULL)) AND ((uibd.Id_Developer = bdn.idDiscoverer_Developer) OR ((uibd.Id_Developer) IS NULL AND (bdn.idDiscoverer_Developer) IS NULL)))
WHERE (uibd.Name = 'John')
```

Polymorphism even works if `Name` property is implemented using [explicit interface implementation](http://msdn.microsoft.com/en-us/library/ms173157.aspx) and/or [expression properties](../Signum.Utilities/ExpressionTrees/LinqExtensibility.md).

As a general advice, try to avoid polymorphism and move all the common data to a common entity, using `ImplementedBy` only for the diferent fields (extension pattern). 
