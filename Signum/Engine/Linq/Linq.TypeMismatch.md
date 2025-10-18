## LINQ Type Mismatch

Signum Framework aims to minimize type mismatches between C# and SQL when generating tables, but some differences remain—especially around null handling.

### Null Handling Differences

C# (since version 8) supports nullable reference types, allowing you to explicitly mark reference types as nullable (`string?`). Accessing a member on a `null` reference throws a `NullReferenceException`.

SQL, however, propagates `null` values implicitly. This can cause issues when projecting SQL results into C# value types.

For example:

```csharp
Database.Query<BugEntity>().Select(a => a.Fixer!.Id).ToList();
```

This query returns a `List<PrimaryKey>`. If `Fixer` is `null` for any `BugEntity`, the query will throw an exception, since `PrimaryKey` is an struct and cannot represent `null`.

### FieldReaderException

If you project a non-nullable value from a potentially `null` relationship, you will get a `FieldReaderException`:

```
Data is Null. This method or property cannot be called on Null values.
Ordinal: 2
ColumnName: FixerId
Row: 0
Calling: row.Reader.GetInt32(2)
Projector:
    row => new <>f__AnonymousType0<int,string,int,string>(Id = row.Reader.GetInt32(0), Description = row.Reader.GetString(1), FixerId = row.Reader.GetInt32(2), FixerName = row.Reader.GetString(3))
Command:
    SELECT bdn.Id, bdn.Description, bdn.FixerId, ddn.Name
    FROM BugEntity AS bdn
    LEFT OUTER JOIN DeveloperEntity AS ddn
      ON (bdn.FixerId = ddn.Id)
```

#### Exception Details
* **Ordinal**: Column position
* **ColumnName**: Name of the column
* **Row**: Row number
* **Calling**: Expression accessing the value
* **Projector**: C# expression generating the result
* **Command**: SQL query executed

### Troubleshooting FieldReaderException

1. Check **ColumnName** to see why it is `null`.
2. If `null` is not valid, filter out such results:
   ```csharp
   .Where(b => b.Fixer != null)
   ```
3. If `null` is valid, project to a nullable type using an explicit cast to a nullable type:
   ```csharp
   .Select(b => new {
       b.Id,
       b.Description,
       FixerId = (PrimaryKey?)b.Fixer!.Id,
       FixerName = b.Fixer.Name
   })
   ```
   **Note:** The null-propagating operator (`?.`) is still not supported in expression trees (such as those used by the Signum LINQ provider), until then you will need to use `!.` with explicit casts.

### Best Practices

- Use nullable value types (`int?`) when projecting from relationships that may be `null`.
- Use explicit casts to nullable types in LINQ queries; do not use the null-propagating operator (`?.`) in expression trees.
- Filter out unwanted `null` results when necessary.

C# now enforces nullability more strictly, helping you avoid mismatches. Learn to handle `FieldReaderException` by adjusting your queries as shown above.


