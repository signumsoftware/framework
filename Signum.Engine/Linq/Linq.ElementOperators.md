# LINQ Element Operators

Element operators is the name that Microsoft gives to this group of LINQ methods: `First`, `FirstOrDefault`, `Single` and `SingleOrDefault`. 


## In-Memory behavior: 

The behavior of this methods in-memory is clear: 


|				|0 elements	|1 element	|N elements
|---------------|:---------:|:---------:|:---------:
|FirstOrDefault	| null		| element	| first element
|First			| EXCEPTION	| element	| first element
|SingleOrDefault| null	    | element	| EXCEPTION
|Single			| EXCEPTION	| element	| EXCEPTION

Example: 

```C#
List<ProjectEntity> projects = //...
project.Single();
```

And `LINQ to Signum` also does exactly that, but only **if the query ends in one of this operators**. Example:

```C#
Database.Query<ProjectEntity>().Single(); //Throws EXCEPTION if 0 or N elements
```

The problem comes when the operator is in the middle of a LINQ to Signum query: 


## In-Database behavior: 

In the middle of a LINQ to Signum query there's no easy way to throw an exception, for example: 

```C#
from b in Database.Query<BugEntity>()
let c = b.Comments.Single()
select new { b.Description, c.Text }; 
```

This query is going to be translated to some SQL, but is not cost-effective to throw exceptions for a particular `BugEntity` with more (or less) than just one comment. 

One alternative that we consider was to translate the four operators to the behavior of `FirsOrDefault` when in-database, using `OUTER APPLY` and `TOP(1)`: 

```SQL
SELECT bdn.Description, s2.Text
FROM BugEntity AS bdn
OUTER APPLY (
  (SELECT TOP (1) bdnc.Text
  FROM BugDNComments AS bdnc
  WHERE (bdn.Id = bdnc.idParent))
) AS s2
```

But having the same translation for the fourth operators is a loss of expressiveness:

* If you know that there will be **always more than zero related rows**, the `OUTER APPLY` could be replaced by `CROSS APPLY`. 
* If you know that there will **never be more than one related row**, so the `TOP(1)` is unnecessary. 

So what we did instead is to re-interpret the behavior of the four operators, replacing **EXCEPTION** -> **QRMNS** (Query Results Make No Sense).   

|				|0 elements	|1 element	|N elements
|---------------|:---------:|:---------:|:---------:
|FirstOrDefault	| null		| element	| first element
|First			| QRMNS	    | element	| first element
|SingleOrDefault| null	    | element	| QRMNS
|Single			| QRMNS	    | element	| QRMNS

So when you write:

```C#
from b in Database.Query<BugEntity>()
let c = b.Comments.Single()
select new { b.Description, c.Text }; 
```

It will be translated to:
 
```SQL
SELECT bdn.Description, s1.Text
FROM BugEntity AS bdn
CROSS APPLY (
  (SELECT bdnc.Text
  FROM BugDNComments AS bdnc
  WHERE (bdn.Id = bdnc.idParent))
) AS s1
```

This is a beautiful simple query, like the one you could write by hand if you know that there's always exactly one comment for each bug. But notice that, even if `let` is not meant to increase or reduce the number of results:

* If there are bugs without comments, **The bugs itself will disappear!!**
* IF there are bugs with more than one comment, **The bugs will be duplicated!!**.


In other words, you'll be able to write slightly cleaner queries, but think that instead of getting exceptions in the error cases, you'll get malformed queries.
 * If you know that there will never be more than one element *(i.e: There's a UniqueIndex)* you can use `SingleXXX` instead of `FirstXXX`. 
 * If you know that there will always be some elements and null is impossible, feel free to remove `OrDefault`. Except in a `expressionMethod`. 

### Avoid `Single` in `expressionMethod` 

Avoid using `Single` if you're writing an [`expressionMethod`](../Signum.Utilities/ExpressionsTrees/LinqExtensibility.md) since could be an extension over a potentially `null` object. 

Example:

Imagine we have an inverted relationship between `BodyEntity` and `HeadEntity`. The `HeadEntity` refers to the `BodyEntity` with his `Body` column, that has a `UniqueIndex`. This looks like a sensible `expressionMethod`: 

```C#
static Expression<Func<BodyEntity, HeadEntity>> HeadExpression = 
    p => Database.Query<HeadEntity>().Single(h=>h.Body == b); 
[ExpressionField] 
public static HeadEntity Head(this BodyEntity b)
{
    return HeadExpression.Evaluate(b);
}
```

 * We can use `SingleOrDefault` instead of `FirstOrDefault` because we know that there not be two `HeadEntity` pointing to the same `Body` (thanks to the `UniqueIndex` in `HeadEntity.Body`).
 * If we are sure that there will always be a `HeadEntity` for each `BodyEntity`, we could be tempted to use `Single` instead of `SingleOrDefault`, but **avoid this in expression** because you have no control of `BodyEntity b` being `null`. 
 
Imagine that a `CarEntity` has an optional `BodyEntity Driver` property and someone writes this query: 

```C#
Database.Query<CarEntity>().Select(c => c.Driver.Head()).ToList();
```

The writer of this query doesn't know the implementation of `Head`, and will expect this query to return `null` for the parked cars without driver, but if we implement `Head` using `Single`, the **parked cars will disappear!**

So, as a general rule, choose `FirstOrDefault`, `First`, `SingleOrDefault` or `Single` in queries you have total control of, but avoid `Single` on `expressionsMethod` since the input parameter could be `null`.

Finally, **if you think this is just too complicated**, use `FirstOrDefault` always inside of the queries, it's the only operator of the four ones that can be accurately translated to SQL.

### SingleEx, SingleOrDefaultEx, FirstEx

Signum.Utilities defines alternative methods for Single, SingleOrDefault and First in [EnumerableExtensions](../Signum.Utilities/Extensions/EnumerableExtensions.md) that return more expressive exception messages than the BCL counterparts. 

All this methods are also equally supported in the LINQ provider.


