## LINQ (Entity support)

You can mix values, entities, and `Lite<T>` in your results when calling Linq Queries. 


```C#
var result = from b in Database.Query<BugDN>()
             select new { b.Status, b.Fixer, b.Project };
```

Producing the Sql:

```SQL
SELECT bdn.idStatus, ddn.Id, ddn.Ticks, ddn.Name, ddn.ToStr, bdn.idFixer, bdn.idProject, pdn.ToStr AS ToStr1
FROM BugDN AS bdn
LEFT OUTER JOIN DeveloperDN AS ddn
  ON (bdn.idFixer = ddn.Id)
LEFT OUTER JOIN ProjectDN AS pdn
  ON (bdn.idProject = pdn.Id)
```

* **Enum:** Just the `idStatus` is enough to retrieve the `enum` value. 
* **Entity:** The query has been expanded to include all the columns from `DeveloperDN` table, in order to retrieve the `DeveloperDN` object. A
* **Lite\<T>:** Also a join has been made to `ProjectDN` table, but just to retrieve the `ToStr` column.  

In this case, retrieving a `DeveloperDN` was just a few columns because is a simple entity but let's try a bigger entity likeBugDN: 

```C#
var result = from b in Database.Query<BugDN>()
             select b;
```

Surprisingly, this simpler query creates a much bigger generated SQL:

```SQL
--------- MAIN QUERY ------------------------

SELECT bdn.Id, bdn.Ticks, bdn.Description, bdn.Start, bdn.[End], bdn.Hours, bdn.idStatus, cdn.Id AS Id1, cdn.Ticks AS Ticks1, cdn.Name, cdn.ToStr, bdn.idDiscoverer_Customer, ddn.Id AS Id2, ddn.Ticks AS Ticks2, ddn.Name AS Name1, ddn.ToStr AS ToStr1, bdn.idDiscoverer_Developer, ddn1.Id AS Id3, ddn1.Ticks AS Ticks3, ddn1.Name AS Name2, ddn1.ToStr AS ToStr2, bdn.idFixer, bdn.idProject, pdn.ToStr AS ToStr3, bdn.ToStr AS ToStr4
FROM BugDN AS bdn
LEFT OUTER JOIN CustomerDN AS cdn
  ON (bdn.idDiscoverer_Customer = cdn.Id)
LEFT OUTER JOIN DeveloperDN AS ddn
  ON (bdn.idDiscoverer_Developer = ddn.Id)
LEFT OUTER JOIN DeveloperDN AS ddn1
  ON (bdn.idFixer = ddn1.Id)
LEFT OUTER JOIN ProjectDN AS pdn
  ON (bdn.idProject = pdn.Id)

--------- Lazy Client Joins (if needed) -----

SELECT bdn.Id, s3b.Text, s3b.Date, s3b.Id AS Id1, s3b.Ticks, s3b.Name, s3b.ToStr, s3b.idWriter_Customer, s3b.Id1 AS Id11, s3b.Ticks1, s3b.Name1, s3b.ToStr1, s3b.idWriter_Developer, s3b.HasValue, s3b.Id2
FROM BugDN AS bdn
CROSS APPLY (
  (SELECT bdncb.Text, bdncb.Date, cdn1b.Id, cdn1b.Ticks, cdn1b.Name, cdn1b.ToStr, bdncb.idWriter_Customer, ddn2b.Id AS Id1, ddn2b.Ticks AS Ticks1, ddn2b.Name AS Name1, ddn2b.ToStr AS ToStr1, bdncb.idWriter_Developer, bdncb.HasValue, bdncb.Id AS Id2
  FROM BugDNComments AS bdncb
  LEFT OUTER JOIN CustomerDN AS cdn1b
    ON (bdncb.idWriter_Customer = cdn1b.Id)
  LEFT OUTER JOIN DeveloperDN AS ddn2b
    ON (bdncb.idWriter_Developer = ddn2b.Id)
  WHERE (bdn.Id = bdncb.idParent))
) AS s3b
``` 

The first chunk retrieves the `BugDN` itself, together with the related entities like the `Fixer` (a `Developer`),  the `Discoverer` (`CustomerDN` or `DeveloperDN`) and the `Project`. 

The second chung retrieves all the comments and the writers of each comments (also a `CustomerDN` or `DeveloperDN`).

Take into account that, in the absense of `Lite<T>` relationships, the retrieval of the related entities will be eager. This is by design in order to facilitate validation of entities and serialization to other processes (Windows Client), but can cause performance problems if the entities and the queries are not designed carefully. 


## Dot Join (Implicit Joins)

One important advantage of LINQ over plain SQL is that for most of the joins, those across foreign keys, you don't even have to use a join, you can navigate from one table to the related one just using C# dot (`.`) operator. When you try to use a field from an entity in a table that is not in the query, an implicit `LEFT OUTER JOIN` is made automatically. Queries become shorter and more readable this way.

```C#
//instead of this 
var query1 = from b in Database.Query<BugDN>()
             join d in Database.Query<DeveloperDN>() on b.Fixer.Id equals d.Id
             select new { b.Description, d.Name }; 

//you can write this
var query2 = from b in Database.Query<BugDN>()
             select new { b.Description, b.Fixer.Name };
```

However, the behavior of both queries is slightly different. We use `LEFT OUTER JOIN` for implicit joins because we don't want the results to decrease just because you have used an implicit join somewhere.

```SQL
SELECT bdn.Description, ddn.Name
FROM BugDN AS bdn
INNER JOIN DeveloperDN AS ddn
  ON (bdn.idFixer = ddn.Id)

SELECT bdn.Description, ddn.Name
FROM BugDN AS bdn
LEFT OUTER JOIN DeveloperDN AS ddn
  ON (bdn.idFixer = ddn.Id)
```

If you need it, one natural way to reduce the results will  be: 

```C#
var result = from b in Database.Query<BugDN>()
             where b.Fixer != null
             select new { b.Description, b.Fixer.Name }; 
```

That translates to: 

```SQL
SELECT bdn.Description, ddn.Name
FROM BugDN AS bdn
LEFT OUTER JOIN DeveloperDN AS ddn
  ON (bdn.idFixer = ddn.Id)
WHERE (bdn.idFixer) IS NOT NULL
```

## Entity Equality

### Safer than comparing Ids

You can compare two entities using `==` operator (or `!=` operation) and an automatic identity comparison will be done for you. **This is safer than using Ids since you cant get the type wrong.**

```C#
public IQueryable<BugDN> DeveloperBugs(DeveloperDN dev)
{
    return Database.Query<BugDN>().Where(b => b.Id == dev.Id); //BUG HARD TO SPOT!!!!!
    return Database.Query<BugDN>().Where(b => b == dev); //Bug easy to spot: compile error :)
    return Database.Query<BugDN>().Where(b => b.Fixer == dev); //Works!
}
```

Notice how we are mixing in-memory parameters with the query, for example the query: 

```C#
DeveloperDN dev = new DeveloperDN { Name = "John" }.Save();
var bug = from b in Database.Query<BugDN>()
          where b.Fixer == dev
          select new { b.Description, b.Hours };
```

Will translate to:

```SQL
SELECT bdn.Description, bdn.Hours
FROM BugDN AS bdn
WHERE (bdn.idFixer = 2)
```

> **Note:** For the sake of simplicity I'm removing `SqlParameters` in the SQL examples, but don't worry, you are 100% safe from SQL injection attacks while using Linq to Signum

### Polymorphic Entity Equality

Also, this identity comparison even works with simple, `ImplementedBy` and `ImplementedByAll` references in any combinations so you should use this feature to increase your queries' simplicity and make them more robust against `Schema` changes. 

Let's compare the two polymorphic references, `Discoverer` and `Writer`, each of type `Customer` or `Developer`:

```C#
var discovererComments = from b in Database.Query<BugDN>()
                         from c in b.Comments
                         where b.Discoverer == c.Writer
                         select c.Date;
```

And look how the generated query is smart enough to compare the two implementation columns: 

```SQL
SELECT s1.Date
FROM BugDN AS bdn
CROSS APPLY (
  (SELECT bdnc.Date, bdnc.idWriter_Customer, bdnc.idWriter_Developer
  FROM BugDNComments AS bdnc
  WHERE (bdn.Id = bdnc.idParent))
) AS s1
WHERE ((bdn.idDiscoverer_Customer = s1.idWriter_Customer) OR (bdn.idDiscoverer_Developer = s1.idWriter_Developer))
```

There are more complex cases, comparing an arbitrary `ImplementedBy` reference against a `ImplementedByAll` reference would be pain in the neck if done manually, so use this feature! It saves you problems and you will write safer code. 

### `==` vs `Is`
`Entity` overrides `Equals` (and `GetHashCode`) to compare by `Id` and `Type`, but does not overload `==` operator. 

That means that, in memory, `==` means referential equality but `object.Equals` return true if two different instances have the same type an Id. 

Unfortunately, calling `.Equals` in C# is prone to `NullReferenceException` if some object is null, and `object.Equals` static method is just too long, so we added `Is` extension method. 

```C#
entititB == entititB; //only referential equality

entityA.Equals(entititB); //throws NullReferenceException if entityA is null

object.Equals(entityA, entityB); // too long

entityA.Is(entititB); //similar to object.Equals but sorter
```

Both `==` and `Is` methods are supported in the LINQ provider, but not `Equals` or `object.Equals`. The semantics in both cases are the same, because referential equality makes no sense at the database level. 

