## LINQ `SelectMany` Differences

`SelectMany` is a simple operator, but can be difficult to grasp for beginners. For more information on how `SelectMany` works in LINQ, see the [official Microsoft documentation](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/linq/query-expression-syntax-for-selectmany).

One important difference in the implementation of `SelectMany` in LINQ to Signum compared to LINQ to SQL is that LINQ to SQL tries to reduce `CROSS APPLY` operations into `CROSS JOIN` opportunistically. Signum LINQ does not perform this optimization. Instead, it always uses `CROSS APPLY` whenever a `SelectMany` appears.

For example, consider the following query:

```csharp
IQueryable<CommentEntity> result = Database.Query<BugEntity>().SelectMany(b => b.Comments);

// Or the equivalent using query expressions
IQueryable<CommentEntity> result = from b in Database.Query<BugEntity>()
                                   from c in b.Comments    
                                   select c;
```

This will always be translated to a `CROSS APPLY` in SQL:

```sql
SELECT s1.Text, s1.Date, cdn.Id, cdn.Ticks, cdn.Name, s1.WriterId_Customer, ddn.Id AS Id1, ddn.Ticks AS Ticks1, ddn.Name AS Name1, s1.WriterId_Developer, s1.HasValue
FROM BugEntity AS bdn
CROSS APPLY (
  SELECT bdnc.Text, bdnc.Date, bdnc.WriterId_Customer, bdnc.WriterId_Developer, bdnc.HasValue
  FROM BugDNComments AS bdnc
  WHERE bdn.Id = bdnc.ParentId
) AS s1
LEFT OUTER JOIN CustomerEntity AS cdn
  ON s1.WriterId_Customer = cdn.Id
LEFT OUTER JOIN DeveloperEntity AS ddn
  ON s1.WriterId_Developer = ddn.Id
```

If you want LINQ to Signum to work on other DBMSs, such as PostgreSQL, MySQL, or Oracle, this behavior may present challenges, as not all databases support `CROSS APPLY`.

The second difference is not just an implementation detail:

Signum LINQ supports `OUTER APPLY` by adding `DefaultIfEmpty` at the end of the selector lambda.

For example:

```csharp
IQueryable<CommentEntity> result = from b in Database.Query<BugEntity>()
                                   from c in b.Comments.DefaultIfEmpty()    
                                   select c;
```

This will return one `null` element for each `BugEntity` that has no `Comments`, instead of skipping it. This is achieved by using `OUTER APPLY` instead of `CROSS APPLY`:

```sql
SELECT s1.Text, s1.Date, cdn.Id, cdn.Ticks, cdn.Name, s1.WriterId_Customer, ddn.Id AS Id1, ddn.Ticks AS Ticks1, ddn.Name AS Name1, s1.WriterId_Developer, s1.HasValue
FROM BugEntity AS bdn
OUTER APPLY (
  SELECT bdnc.Text, bdnc.Date, bdnc.WriterId_Customer, bdnc.WriterId_Developer, bdnc.HasValue
  FROM BugDNComments AS bdnc
  WHERE bdn.Id = bdnc.ParentId
) AS s1
LEFT OUTER JOIN CustomerEntity AS cdn
  ON s1.WriterId_Customer = cdn.Id
LEFT OUTER JOIN DeveloperEntity AS ddn
  ON s1.WriterId_Developer = ddn.Id
