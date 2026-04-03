# LINQ Inheritance

## Inheritance Support
Signum Framework uses polymorphic foreign keys to implement [inheritance](../../Entities/Inheritance.md), and LINQ to Signum supports all the expected operations naturally:

### `is` Operator

The `is` operator lets you test if an object instance *is* of a certain type. You can also use it in queries:

```csharp
var result = from b in Database.Query<BugEntity>()
             where b.Discoverer is DeveloperEntity
             select b.Description;
```

Translates to:

```sql
SELECT bdn.Description
FROM BugEntity AS bdn
WHERE (bdn.idDiscoverer_Developer) IS NOT NULL
```

> **Note:** Don't confuse the C# `is` operator (like in `fruit is OrangeEntity`) with the `.Is()` extension method to compare entities (like `person.Is(john)`).

### Casting

C# has two ways of casting: typical casting using parentheses `(DeveloperEntity)` and the `as` operator. In case of an invalid conversion, casting throws an `InvalidCastException`, while `as` returns `null`.

At the database level, it makes no sense to throw an `InvalidCastException` for a particular row, so both are implemented similarly to the `as` operator.

So, these two queries are similar:

```csharp
var result = from b in Database.Query<BugEntity>()
             select ((DeveloperEntity)b.Discoverer).Name;

var result = from b in Database.Query<BugEntity>()
             select (b.Discoverer as DeveloperEntity).Name;
```

Additionally, since implicit joins are implemented as `LEFT OUTER JOIN`, nulls propagate without throwing a `NullReferenceException`. So the result, if in-memory, would be something like:

```csharp
var result = from b in Database.Query<BugEntity>()
             select b.Discoverer is DeveloperEntity ? ((DeveloperEntity)b.Discoverer).Name : null;
```

Or using the null-propagating operator `?.`:

```csharp
var result = from b in Database.Query<BugEntity>()
             select (b.Discoverer as DeveloperEntity)?.Name;
```

> **Note:** Unfortunately, `?.` has not yet been implemented in C# for expression trees.

### `GetType`

Using the `GetType` method and comparing or returning `System.Type` objects is also fully supported. For example:

```csharp
var result = from b in Database.Query<BugEntity>()
             where b.Discoverer.GetType() == typeof(DeveloperEntity)
             select b.Project.EntityType;
```

Is translated to:

```sql
SELECT bdn.idProject
FROM BugEntity AS bdn
WHERE (bdn.idDiscoverer_Developer) IS NOT NULL
```

And returns a `List<System.Type>`, with all the elements being `typeof(ProjectEntity)`.

### Polymorphism

Signum Framework also supports polymorphism for `ImplementedBy` expressions, so you can translate this query even if the `Name` property is declared in the `IBugDiscoverer` interface:

```csharp
var result = from b in Database.Query<BugEntity>()
             where b.Discoverer.Name == "John"
             select b.Description;
```

The translation joins with the two implementations and uses a `CASE` expression to combine the results:

```sql
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
  END = 'John')
```

> **Warning:** In our experience, this type of complex condition using `OR` or `CASE` expressions in SQL Server tends to be slow for large data sets. So **don't abuse them!**

Sometimes the query can be faster using a `UNION` strategy instead of a `CASE`, but we haven't found a heuristic to make the choice automatic. In the meantime, you can use the `CombineUnion` extension method over any `ImplementedBy` property to change the behavior:

```csharp
var result = from b in Database.Query<BugEntity>()
             where b.Discoverer.CombineUnion().Name == "John"
             select b.Description;
```

This translates to:

```sql
SELECT bdn.Description
FROM BugEntity AS bdn
LEFT OUTER JOIN (
    (SELECT cdn.Id AS Id_Customer, NULL AS Id_Developer, cdn.Name
     FROM CustomerEntity AS cdn)
  UNION ALL
    (SELECT NULL AS Id_Customer, ddn.Id AS Id_Developer, ddn.Name
     FROM DeveloperEntity AS ddn)
  ) AS uibd
  ON (((uibd.Id_Customer = bdn.idDiscoverer_Customer) OR ((uibd.Id_Customer) IS NULL AND (bdn.idDiscoverer_Customer) IS NULL))
      AND ((uibd.Id_Developer = bdn.idDiscoverer_Developer) OR ((uibd.Id_Developer) IS NULL AND (bdn.idDiscoverer_Developer) IS NULL)))
WHERE (uibd.Name = 'John')
```

Polymorphism even works if the `Name` property is implemented using [explicit interface implementation](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/interfaces/explicit-interface-implementation) and/or [expression properties](../Signum.Utilities/ExpressionTrees/LinqExtensibility.md).

> **Advice:** Try to avoid polymorphism and move all common data to a common entity, using `ImplementedBy` only for the different fields (extension pattern).
