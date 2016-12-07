## LINQ `SelectMany` differences

`SelectMany` is a very simple operator, however, is sometimes hard to grasp for beginners. You can see more about how `SelectMany` is meant to work [here](http://www.hookedonlinq.com/SelectManyOperator.ashx). 


On important difference of the implementation `SelectMany` in LINQ to Signum over Linq to SQL is that a big part of Linq to Sql code base is, in [words of Matt Warren](http://blogs.msdn.com/b/mattwar/archive/2007/09/04/linq-building-an-iqueryable-provider-part-vii.aspx):

> to reduce `CROSS APPLY`'s into `CROSS JOIN` in an opportunistic fashion. 

We save ourselves from writing this code, instead we use `CROSS APPLY` any time a `SelectMany` appears.

So if you write a query like this

```C#
IQueryable<CommentEntity> result = Database.Query<BugEntity>().SelectMany(b => b.Comments);

//Or the equivalent using query expressions
IQueryable<CommentEntity> result = from b in Database.Query<BugEntity>()
                               from c in b.Comments    
                               select c;
```

`SelectMany` will always translated to `CROSS APPLY`. 

```SQL
SELECT s1.Text, s1.Date, cdn.Id, cdn.Ticks, cdn.Name, s1.idWriter_Customer, ddn.Id AS Id1, ddn.Ticks AS Ticks1, ddn.Name AS Name1, s1.idWriter_Developer, s1.HasValue
FROM BugEntity AS bdn
CROSS APPLY (
  (SELECT bdnc.Text, bdnc.Date, bdnc.idWriter_Customer, bdnc.idWriter_Developer, bdnc.HasValue
  FROM BugDNComments AS bdnc
  WHERE (bdn.Id = bdnc.idParent))
) AS s1
LEFT OUTER JOIN CustomerEntity AS cdn
  ON (s1.idWriter_Customer = cdn.Id)
LEFT OUTER JOIN DeveloperEntity AS ddn
  ON (s1.idWriter_Developer = ddn.Id)
```

If someone wants LINQ to Signum to work on other DBMS, like PostgreSQL, MySQL or Oracle, this will be a difficult part.

The second difference is not an implementation detail like the firts one:

We support `OUTER APPLY` by adding `DefaultIfEmpty` at the end of the selector lambda.

So a query like this: 

```C#
IQueryable<CommentEntity> result = from b in Database.Query<BugEntity>()
                               from c in b.Comments.DefaultIfEmpty()    
                               select c;
```

Will return one `null` element for each `BugsEntity` that have no `Comments`, instead of just skipping it. 

This is thanks to the use of `OUTER APPLY` instead of `CROSS APPLY`. 

```SQL
SELECT s1.Text, s1.Date, cdn.Id, cdn.Ticks, cdn.Name, s1.idWriter_Customer, ddn.Id AS Id1, ddn.Ticks AS Ticks1, ddn.Name AS Name1, s1.idWriter_Developer, s1.HasValue
FROM BugEntity AS bdn
OUTER APPLY (
  (SELECT bdnc.Text, bdnc.Date, bdnc.idWriter_Customer, bdnc.idWriter_Developer, bdnc.HasValue
  FROM BugDNComments AS bdnc
  WHERE (bdn.Id = bdnc.idParent))
) AS s1
LEFT OUTER JOIN CustomerEntity AS cdn
  ON (s1.idWriter_Customer = cdn.Id)
LEFT OUTER JOIN DeveloperEntity AS ddn
  ON (s1.idWriter_Developer = ddn.Id)
```