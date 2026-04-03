# LINQ `Lite<T>` Support

`Lite<T>` is fully supported in Signum LINQ queries. If you are unfamiliar with `Lite<T>`, see the [Lite documentation](../../Signum/Entities/Lite.md).

## Navigating `Lite<T>` Relationships

`Lite<T>` objects indicate when to load an entity eagerly or lazily, and help reduce the entity graph sent to the client (Windows/React). In the SQL schema, there is no difference between a property of type `Lite<T>` or `T`: both create a foreign key to the related table, and the convention is to use `PropertyId` (not `idProperty`). In Signum LINQ queries, you can access the underlying entity using the `Entity` or `EntityOrNull` property of `Lite<T>`.

```csharp
var result = from b in Database.Query<BugEntity>()
             select new
             {
                 b.Description,
                 b.Project.Entity.Name
             };
```

This translates to:

```sql
SELECT bdn.Description, pdn.Name
FROM BugEntity AS bdn
LEFT OUTER JOIN ProjectEntity AS pdn
  ON (bdn.ProjectId = pdn.Id)
```

> **Note:** To avoid confusion, the `Retrieve` method is not supported in queries, as you are already querying the database. For example, the following will throw an exception:

```csharp
from b in Database.Query<BugEntity>()
where b.Project.Retrieve().Name == "Framework"
select new { b.Description };
// Throws InvalidOperationException: "The expression can not be translated to SQL: new ProjectEntity(bdn.ProjectId)"
```

## Using `Lite<T>` as a Query Result

You can use `Lite<T>` objects as results in LINQ queries. Create them explicitly with `.ToLite()` on any entity, or use them directly if your model already uses `Lite<T>` properties:

```csharp
// The first property is an explicit Lite, the second is already a Lite<ProjectEntity>.
var result = from b in Database.Query<BugEntity>()
             select new
             {
                 Bug = b.ToLite(), // Explicit ToLite() because b is a BugEntity
                 Project = b.Project // Already a Lite<ProjectEntity>
             };
```

This translates to:

```sql
SELECT bdn.Id, bdn.Description, bdn.ProjectId, pdn.Name
FROM BugEntity AS bdn
LEFT OUTER JOIN ProjectEntity AS pdn
  ON (bdn.ProjectId = pdn.Id)
```

> **Note:** The `ToLite` method is defined in the static `Lite` class in `Signum.Entities`.

This also works with `ImplementedBy` and `ImplementedByAll` references:

```csharp
var result = from b in Database.Query<BugEntity>()
             select b.Discoverer.ToLite(); // Polymorphic Lite
```

If the `ToString` property is implemented using `[AutoExpressionField]`, the columns used in the expression will be selected instead of the `ToStr` column. For example, if `ToString` is defined as `Name`, then `Name` will be selected in the SQL, not `ToStr`.

This joins to the different implementations to get the correct columns:

```sql
SELECT bdn.DiscovererId_Customer, bdn.DiscovererId_Developer, cdn.Name, ddn.Name AS Name1
FROM BugEntity AS bdn
LEFT OUTER JOIN CustomerEntity AS cdn
  ON (bdn.DiscovererId_Customer = cdn.Id)
LEFT OUTER JOIN DeveloperEntity AS ddn
  ON (bdn.DiscovererId_Developer = ddn.Id)
```

The result will be statically typed as `List<Lite<IBugDiscoverer>>`, but at runtime, elements will be either `Lite<CustomerEntity>` or `Lite<DeveloperEntity>`.

## Comparing `Lite<T>`

Like entities, `Lite<T>` can be compared to `null` using `==` and `!=`, and to other entities or lites using the `Is` extension method. This prevents unintended reference equality checks.

```csharp
Lite<DeveloperEntity> dev = ...;

var discovererComments = from b in Database.Query<BugEntity>()
                         where b.Discoverer.ToLite().Is(dev)
                         select b.Date;
```

You can also use `Is` to compare a `Lite<T>` with a `T`:

```csharp
Lite<DeveloperEntity> dev = ...;

var discovererComments = from b in Database.Query<BugEntity>()
                         where dev.Is(b.Discoverer)
                         select b.Date;
```

> **Warning:** Due to `Lite<T>` being covariant and implemented as an interface, the C# compiler allows comparing `Lite<T>` with `T`, which will compile but throw an exception at runtime:

```csharp
Lite<DeveloperEntity> dev = ...;

var result = from b in Database.Query<BugEntity>()
             select b.Discoverer.Is(dev); // Polymorphic Lite
// Throws InvalidOperationException: "Impossible to compare expressions of type IBugDiscoverer == Lite<DeveloperEntity>"
```

> **Note:** The Signum.Analyzer restores compile-time errors when it detects comparisons between incompatible types, such as `Lite<OrangeEntity>` and `AppleEntity`, or `Lite<OrangeEntity>` and `Lite<AppleEntity>`.

