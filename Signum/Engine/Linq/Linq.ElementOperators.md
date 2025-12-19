# LINQ Element Operators

Element operators in LINQ include: `First`, `FirstOrDefault`, `Single`, and `SingleOrDefault`.

## In-Memory Behavior

The behavior of these methods in-memory is straightforward:

|                | 0 elements | 1 element | N elements |
|----------------|:----------:|:---------:|:----------:|
| FirstOrDefault | null       | element   | first      |
| First          | EXCEPTION  | element   | first      |
| SingleOrDefault| null       | element   | EXCEPTION  |
| Single         | EXCEPTION  | element   | EXCEPTION  |

Example:

```csharp
List<ProjectEntity> projects = // ...
projects.Single();
```

The Signum LINQ provider matches this behavior **when the query ends with one of these operators**:

```csharp
Database.Query<ProjectEntity>().Single(); // Throws EXCEPTION if 0 or N elements
```

## In-Database Behavior

When an element operator appears in the middle of a LINQ to Signum query, throwing exceptions for specific entities is not practical. For example:

```csharp
from b in Database.Query<BugEntity>()
let c = b.Comments.Single()
select new { b.Description, c.Text };
```

This query translates to SQL, but handling exceptions for each `BugEntity` with more or fewer than one comment is inefficient. One option is to translate all four operators to the behavior of `FirstOrDefault` using `OUTER APPLY` and `TOP(1)`:

```sql
SELECT bdn.Description, s2.Text
FROM BugEntity AS bdn
OUTER APPLY (
  SELECT TOP (1) bdnc.Text
  FROM BugDNComments AS bdnc
  WHERE bdn.Id = bdnc.idParent
) AS s2
```

However, this approach loses expressiveness:
- If you know there will **always be at least one related row**, use `CROSS APPLY` instead of `OUTER APPLY`.
- If you know there will **never be more than one related row**, `TOP(1)` is unnecessary.

Instead, Signum reinterprets the behavior of these operators in-database, replacing **EXCEPTION** with **QRMNS** (Query Results Make No Sense):

|                | 0 elements | 1 element | N elements |
|----------------|:----------:|:---------:|:----------:|
| FirstOrDefault | null       | element   | first      |
| First          | QRMNS      | element   | first      |
| SingleOrDefault| null       | element   | QRMNS      |
| Single         | QRMNS      | element   | QRMNS      |

So, the query:

```csharp
from b in Database.Query<BugEntity>()
let c = b.Comments.Single()
select new { b.Description, c.Text };
```

Translates to:

```sql
SELECT bdn.Description, s1.Text
FROM BugEntity AS bdn
CROSS APPLY (
  SELECT bdnc.Text
  FROM BugDNComments AS bdnc
  WHERE bdn.Id = bdnc.idParent
) AS s1
```

This produces a simple query, but note:
- Bugs without comments **disappear** from results.
- Bugs with multiple comments **are duplicated**.

In summary, you get cleaner queries, but instead of exceptions, you may get malformed results. Use the operator that matches your data constraints:
- If there will never be more than one element (e.g., a UniqueIndex), use `SingleXXX`.
- If there will always be elements and `null` is impossible, you can omit `OrDefault` (except in an expression method).

## Avoid `Single` in Expression Methods

Avoid using `Single` in [`expressionMethod`](../../Signum.Utilities/ExpressionTrees/LinqExtensibility.md) since the input may be `null`.

Example:

Suppose `HeadEntity` refers to `PersonEntity` via a unique `Body` column. This is a typical expression method:

```csharp
public static HeadEntity Head(this PersonEntity p) => 
	As.Expression(()=> Database.Query<HeadEntity>().Single(h => h.Body.Is(b)))
```

- Use `SingleOrDefault` instead of `FirstOrDefault` if you know there cannot be two `HeadEntity` pointing to the same `Body` (thanks to the UniqueIndex).
- If you are sure there will always be a `HeadEntity` for each `PersonEntity`, you might use `Single`, but **avoid this in expressions** because the input could be `null`.

For example, if `CarEntity` has an optional `PersonEntity? Driver` property:

```csharp
Database.Query<CarEntity>().Select(c => new 
{ 
	c.LicenseNumber, 
	c.Model, 
	DriverHead = c.Driver!.Head()
}).ToList();
```

The query should return `null` for cars without drivers. If `Head` uses `Single`, parked cars will disappear from results.

**General rule:** Use the operator that matches your control over the query. Avoid `Single` in expression methods where the input may be `null`.

If in doubt, use `FirstOrDefault` inside queries; it is the only operator that can be accurately translated to SQL.

## SingleEx, SingleOrDefaultEx, FirstEx

Signum.Utilities defines alternative methods (`SingleEx`, `SingleOrDefaultEx`, `FirstEx`) in [EnumerableExtensions](../../Signum.Utilities/Extensions/EnumerableExtensions.md) that provide more expressive exception messages than the BCL counterparts. These methods are also supported in the LINQ provider.


