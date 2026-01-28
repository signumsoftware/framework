## LINQ (Entity Support)

Signum LINQ provider allows you to mix values, entities, and `Lite<T>` in your query results.

```csharp
var result = from b in Database.Query<BugEntity>()
             select new { b.Status, b.Fixer, b.Project };
```

This produces SQL similar to:

```sql
SELECT bdn.idStatus, ddn.Id, ddn.Ticks, ddn.Name, ddn.ToStr, bdn.FixerId, bdn.ProjectId, pdn.Name as Name2
FROM BugEntity AS bdn
LEFT OUTER JOIN DeveloperEntity AS ddn ON (bdn.idFixer = ddn.Id)
LEFT OUTER JOIN ProjectEntity AS pdn ON (bdn.idProject = pdn.Id)
```

- **Enum:** Only the `StatusId` column is needed to retrieve the enum value.
- **Entity:** The query expands to include all columns from `DeveloperEntity` to retrieve the full entity.
- **Lite<T>:** A join to `ProjectEntity` is made, but only the `pdn.Name` column is retrieved.

Retrieving a simple entity like `DeveloperEntity` is efficient, but querying a more complex entity like `BugEntity` results in a larger SQL statement:

```csharp
var result = from b in Database.Query<BugEntity>()
             select b;
```

Surprisingly, this simpler query creates a much bigger generated SQL:

```sql
-- Main Query --
SELECT bdn.Id, bdn.Ticks, bdn.Description, bdn.Start, bdn.[End], bdn.Hours, bdn.idStatus, cdn.Id AS Id1, cdn.Ticks AS Ticks1, cdn.Name, cdn.ToStr, bdn.idDiscoverer_Customer, ddn.Id AS Id2, ddn.Ticks AS Ticks2, ddn.Name AS Name1, ddn.ToStr AS ToStr1, bdn.idDiscoverer_Developer, ddn1.Id AS Id3, ddn1.Ticks AS Ticks3, ddn1.Name AS Name2, ddn1.ToStr AS ToStr2, bdn.idFixer, bdn.idProject, pdn.ToStr AS ToStr3, bdn.ToStr AS ToStr4
FROM BugEntity AS bdn
LEFT OUTER JOIN CustomerEntity AS cdn ON (bdn.idDiscoverer_Customer = cdn.Id)
LEFT OUTER JOIN DeveloperEntity AS ddn ON (bdn.idDiscoverer_Developer = ddn.Id)
LEFT OUTER JOIN DeveloperEntity AS ddn1 ON (bdn.idFixer = ddn1.Id)
LEFT OUTER JOIN ProjectEntity AS pdn ON (bdn.idProject = pdn.Id)

-- Lazy Client Joins (if needed) --
SELECT bdn.Id, s3b.Text, s3b.Date, s3b.Id AS Id1, s3b.Ticks, s3b.Name, s3b.ToStr, s3b.idWriter_Customer, s3b.Id1 AS Id11, s3b.Ticks1, s3b.Name1, s3b.ToStr1, s3b.idWriter_Developer, s3b.HasValue, s3b.Id2
FROM BugEntity AS bdn
CROSS APPLY (
  SELECT bdncb.Text, bdncb.Date, cdn1b.Id, cdn1b.Ticks, cdn1b.Name, cdn1b.ToStr, bdncb.idWriter_Customer, ddn2b.Id AS Id1, ddn2b.Ticks AS Ticks1, ddn2b.Name AS Name1, ddn2b.ToStr AS ToStr1, bdncb.idWriter_Developer, bdncb.HasValue, bdncb.Id AS Id2
  FROM BugDNComments AS bdncb
  LEFT OUTER JOIN CustomerEntity AS cdn1b ON (bdncb.idWriter_Customer = cdn1b.Id)
  LEFT OUTER JOIN DeveloperEntity AS ddn2b ON (bdncb.idWriter_Developer = ddn2b.Id)
  WHERE (bdn.Id = bdncb.idParent)
) AS s3b
```

The main query retrieves the `BugEntity` and its related entities (`Fixer`, `Discoverer`, `Project`). The second query retrieves all comments and their writers.

> **Note:** Without `Lite<T>` relationships, related entities are loaded eagerly. This design facilitates validation and serialization, but may impact performance if entities or queries are not carefully designed.

---

## Dot Join (Implicit Joins)

A key advantage of Signum LINQ is implicit joins: you can navigate foreign keys using the C# dot (`.`) operator. If you reference a field from a related entity, a `LEFT OUTER JOIN` is automatically generated.

```csharp
// Instead of:
var query1 = from b in Database.Query<BugEntity>()
             join d in Database.Query<DeveloperEntity>() on b.Fixer.Id equals d.Id
             select new { b.Description, d.Name };

// You can write:
var query2 = from b in Database.Query<BugEntity>()
             select new { b.Description, b.Fixer.Name };
```

The behavior differs slightly:
- Explicit join: `INNER JOIN` (rows only if both sides exist)
- Implicit join: `LEFT OUTER JOIN` (all rows from left, even if right is null)

```sql
-- Explicit join
SELECT bdn.Description, ddn.Name
FROM BugEntity AS bdn
INNER JOIN DeveloperEntity AS ddn ON (bdn.idFixer = ddn.Id)

-- Implicit join
SELECT bdn.Description, ddn.Name
FROM BugEntity AS bdn
LEFT OUTER JOIN DeveloperEntity AS ddn ON (bdn.idFixer = ddn.Id)
```

To filter out nulls:

```csharp
var result = from b in Database.Query<BugEntity>()
             where b.Fixer != null
             select new { b.Description, b.Fixer.Name };
```

Which translates to:

```sql
SELECT bdn.Description, ddn.Name
FROM BugEntity AS bdn
LEFT OUTER JOIN DeveloperEntity AS ddn ON (bdn.idFixer = ddn.Id)
WHERE (bdn.idFixer) IS NOT NULL
```

---

## Entity Equality

You can use in-memory variables in queries:

```csharp
DeveloperEntity dev = new DeveloperEntity { Name = "John" }.Save();
var bug = from b in Database.Query<BugEntity>()
          where b.Fixer.Is(dev)
          select new { b.Description, b.Hours };
```

This generates:

```sql
SELECT bdn.Description, bdn.Hours
FROM BugEntity AS bdn
WHERE (bdn.idFixer = @p1)

@p1 = 2
```

> **Note:** Queries are automatically parameterized, protecting against SQL injection and enabling SQL Server to cache execution plans.


### Comparing ids (Risky)

In general, comparing Ids directly should be avoided, since they could produce subtle bugs when the entity types differ.

```csharp
public IQueryable<BugEntity> DeveloperBugs(DeveloperEntity dev)
{
    return Database.Query<BugEntity>().Where(b => b.Id == dev.Id); // Not recommended: hard to spot bug
}
```

### Comparing Entities with `==` vs `.Is` (Recommended)

A better approach than comparing entity Ids directly is to compare entities using `==`, `!=`, or the `.Is` extension method.

```csharp
public IQueryable<BugEntity> DeveloperBugs(DeveloperEntity dev)
{
    return Database.Query<BugEntity>().Where(b => b == dev); // Safer: compile-time safety
    return Database.Query<BugEntity>().Where(b => b.Fixer == dev); // Safer
}
```

However, in memory, `==` checks referential equality (i.e., whether both variables point to the same instance), not just identity:

```csharp
var bug1 = Database.Query<BugEntity>().Single(b => b.Description == "Some description");
var bug2 = Database.Query<BugEntity>().Single(b => b.Description == "Some description");

bool areEqual = bug1 == bug2; // false, different instances
```

To compare entities by identity (type and Id), use the `.Is` extension method, which works both in-memory and in queries:

```csharp
public IQueryable<BugEntity> DeveloperBugs(DeveloperEntity dev)
{
    return Database.Query<BugEntity>().Where(b => b.Is(dev)); // Recommended: type-safe and works in-memory
    return Database.Query<BugEntity>().Where(b => b.Fixer.Is(dev)); // Recommended
}

bool areEqual = bug1.Is(bug2); // true if same identity
```

Additionally, the `.Is` method handles nulls gracefully:

```csharp
 DeveloperEntity dev = null;
 bool isNull = dev.Is(null); // true
 bool isNotNull = null.Is(dev); // true
```

Even more, you can compare T and Lite<T> and it will work as expected:
```csharp
DeveloperEntity dev = ...;
Lite<DeveloperEntity> liteDev = dev.ToLite();

bool isEqual1 = dev.Is(liteDev); // true
bool isEqual2 = liteDev.Is(dev); // true
```


> `Entity` overrides `Equals` (and `GetHashCode`) for identity comparison, but does not overload `==`. Calling `.Equals` can throw if the object is null, so `.Is` is preferred for brevity and safety. Both `==` and `.Is` are supported in the LINQ provider, but `.Is` is recommended. At the database level, only identity comparison is meaningful.

### Polymorphic Entity Equality

Identity comparison works for simple and polymorphic references (`ImplementedBy`, `ImplementedByAll`). This keeps queries simple and robust against schema changes.

Example comparing two polymorphic references:

```csharp
var discovererComments = from b in Database.Query<BugEntity>()
                         from c in b.Comments
                         where b.Discoverer.Is(c.Writer)
                         select c.Date;
```

Generated SQL:

```sql
SELECT s1.Date
FROM BugEntity AS bdn
CROSS APPLY (
  SELECT bdnc.Date, bdnc.idWriter_Customer, bdnc.idWriter_Developer
  FROM BugDNComments AS bdnc
  WHERE (bdn.Id = bdnc.idParent)
) AS s1
WHERE ((bdn.idDiscoverer_Customer = s1.idWriter_Customer) OR (bdn.idDiscoverer_Developer = s1.idWriter_Developer))
```

Comparing simple references, `ImplementedBy` to `ImplementedByAll` works automatically in-memory and in-queries.

