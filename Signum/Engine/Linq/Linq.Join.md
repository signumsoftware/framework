# LINQ `Join` Differences

LINQ provides a unified model for querying both in-memory objects and databases. However, the standard LINQ join syntax can be cumbersome, especially for common SQL join scenarios. The Signum Framework offers a more SQL-like approach for joins, making queries simpler and more intuitive.

## Joins in LINQ to Objects / LINQ to SQL

In standard LINQ, there are two main join operators:

```csharp
// Joins two sequences based on matching keys and projects the result.
public static IQueryable<TResult> Join<TOuter, TInner, TKey, TResult>(
    this IQueryable<TOuter> outer, 
    IEnumerable<TInner> inner, 
    Expression<Func<TOuter, TKey>> outerKeySelector, 
    Expression<Func<TInner, TKey>> innerKeySelector, 
    Expression<Func<TOuter, TInner, TResult>> resultSelector)

// Groups the results of two sequences based on matching keys.
public static IQueryable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(
    this IQueryable<TOuter> outer, 
    IEnumerable<TInner> inner,
    Expression<Func<TOuter, TKey>> outerKeySelector, 
    Expression<Func<TInner, TKey>> innerKeySelector, 
    Expression<Func<TOuter, IEnumerable<TInner>, TResult>> resultSelector)
```

There is no explicit `OUTER JOIN` operator. Instead, you achieve a `LEFT OUTER JOIN` by combining `GroupJoin`, `SelectMany`, and `DefaultIfEmpty`:

```csharp
using (var db = new BugContext())
{
    var q = from b in db.Bugs
            join c in db.Comments on b.Id equals c.BugId into g
            from c in g.DefaultIfEmpty()
            select new { b.Description, CommentText = c == null ? "(no comment)" : c.Text };
}
```

**Limitations:**
- Requires multiple operators for a simple `LEFT OUTER JOIN`.
- No support for `RIGHT OUTER JOIN` or `FULL OUTER JOIN`.
- The translation depends on how the grouping is used, which can be confusing.

## Joins in Signum LINQ

Signum Framework provides a more SQL-like join syntax. You can use `DefaultIfEmpty()` on either side of the join to specify which side should allow nulls (i.e., which side is 'outer'). This enables `LEFT`, `RIGHT`, and `FULL OUTER JOIN` scenarios directly.

**Example:**

```csharp
var q = from b in Database.Query<BugEntity>()
        join c in Database.Query<CommentEntity>().DefaultIfEmpty() on b equals c.Bug
        select new { b.Description, CommentText = c == null ? "(no comment)" : c.Text };
```

### Types of JOIN

Explicit joins are typically used when joining on non-foreign-key fields. For foreign keys, implicit joins are usually more convenient in Signum.

Suppose you want to join pairs of consecutive bugs by `Start` and `End` date:

#### INNER JOIN

Returns only matched pairs:

```csharp
from b1 in Database.Query<BugEntity>()
join b2 in Database.Query<BugEntity>() on b1.End equals b2.Start
select new { b1, b2 }
```

#### LEFT OUTER JOIN

Returns all `b1` bugs, with `b2` as the next bug or `null`:

```csharp
from b1 in Database.Query<BugEntity>()
join b2 in Database.Query<BugEntity>().DefaultIfEmpty() on b1.End equals b2.Start
select new { b1, b2 }
```

#### RIGHT OUTER JOIN

Returns all `b2` bugs, with `b1` as the previous bug or `null`:

```csharp
from b1 in Database.Query<BugEntity>().DefaultIfEmpty()
join b2 in Database.Query<BugEntity>() on b1.End equals b2.Start
select new { b1, b2 }
```

#### FULL OUTER JOIN

Returns all bugs from both sides, with matching pairs if they exist, or `null` if not:

```csharp
from b1 in Database.Query<BugEntity>().DefaultIfEmpty()
join b2 in Database.Query<BugEntity>().DefaultIfEmpty() on b1.End equals b2.Start
select new { b1, b2 }
```
