## LINQ Type Mismatch

Even if Signum Framework tries to reduce type-mismatch between C# and SQL to the minimum when generating the tables, there are situations where this is currently inevitable. 

Nulls are specially problematic. C# and SQL treat them in a different way. 

In C#, all the reference types are nullable ([for now?](https://roslyn.codeplex.com/discussions/541334)) but they throw a `NullReferenceException` when you try to access any member on a `null` element. 

In SQL this behavior is hard to imitate, and the only sensible behavior is to propagate `null`, much like the [null-propagating operator ?.](https://roslyn.codeplex.com/discussions/540883) but completely implicit.

This mismatch has problems when the result is a ValueType:

```C#
Database.Query<BugEntity>().Select(a => a.Fixer.Id).ToList();
```

The result type of this query is inevitably `List<int>`, and there's nothing LINQ to Signum (or any other provider) can do to change it to `List<int?>`. 

So, if `Fixes` is `null` for some `BugEntity` the options are:

* **Option 1:**  Returns `default(T)` in this case `0`. 
* **Option 2:**  Throws an exception to force you make space for `null` values. 

We went for the second option. It's the most honest, but also the hardest one. 

### FieldReaderException

This means that executing this expression: 

```C#
Database.Query<BugEntity>()
.Select(b => new 
{
    b.Id,
    b.Description,
    FixerId = b.Fixer.Id,
    FixerName = b.Fixer.Name 
}).ToList()
```

Throws an `FieldReaderException` with this message: 

```
Data is Null. This method or property cannot be called on Null values.
Ordinal: 2
ColumnName: idFixer
Row: 0
Calling: row.Reader.GetInt32(2)
Projector:
    row => new <>f__AnonymousType0<int,string,int,string>(Id = row.Reader.GetInt32(0), Description = row.Reader.GetString(1), FixerId = row.Reader.GetInt32(2), FixerName = row.Reader.GetString(3))
Command:
    SELECT bdn.Id, bdn.Description, bdn.idFixer, ddn.Name
    FROM BugEntity AS bdn
    LEFT OUTER JOIN DeveloperEntity AS ddn
      ON (bdn.idFixer = ddn.Id)
```

This messages can be much bigger for more complex queries since it tries to provide a lot of information: 

* **Ordinal**: The column position that was about to be read
* **ColumnName**: The column name that was about to be read
* **Row**: The current row of the result
* **Calling**: The sub-expression calling the `FieldReader` (proxy to the `DataReader`) that was being executed. 
* **Projector**: The complete C# expression that generates the results from the `FieldReader`.
* **Command**: The complete SQL query that was being executed. 


### Troubleshooting FieldReaderException   

When faced with a `FieldReaderException` with message `Data is Null` follow this steps: 

1. To take a look at the **ColumnName**  and why is `null`. 
2. If `null` is not a valid result for your query, filter the results to avoid it.
3. If `null` is a valid value, look for where the **Calling** expression is included in the **Projector** and try to figure out the relationship with your query, casting to the nullable version. 

So now looking at our example: 

1. **ColumnName** is `idFixer` and is `null` because `Fixer` can be `null`
2. If we want to avoid bugs with `Fixer` we will just need to add `.Where(b => b.Fixer != null)` before the select.
3. If instead we want nulls in the result, we see that `row.Reader.GetInt32(2)` is used in `FixerId = row.Reader.GetInt32(2)` so `FixerId` is the property that is not letting nulls get in. Let's change the query to: 

```C#
Database.Query<BugEntity>()
.Select(b => new 
{
    b.Id,
    b.Description,
    FixerId = (int?)b.Fixer.Id,
    FixerName = b.Fixer.Name 
}).ToList()
```

And now it should work properly. In the future version of C# we could write this instead: 

```C#
Database.Query<BugEntity>()
.Select(b => new 
{
    b.Id,
    b.Description,
    FixerId = b.Fixer?.Id,
    FixerName = b.Fixer.Name 
}).ToList()
```

***Note::** Even if C# 6 has support for `?.` it doesn't work yet in queries, so we need to keep waiting... 

In order to completely remove the nullability mismatch problem, C# will need to forbid  `.` operator on nullable reference types (like SWIFT does), and only `?.` will be allowed in queries.

Until this happens so you'll need to learn now to fix `FieldReaderException`.  


