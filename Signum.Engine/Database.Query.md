# Database.Query

Take a comfortable seat, breath deeply and prepare yourself. `Database.Query<T>()` is Signum Engine's gate to the LINQ world, a world of elegant compiled-time checked and IntelliSense assisted queries that will empower you to a new level of productivity. 

By taking ideas from functional languages (like lambdas and expression trees) with the very practical purpose of integrating queries in the language, Microsoft has completely changed the way enterprise code has to be done. Anyone who has used LINQ in some of its' flavors just won't turn back.

# A bit of context
The design that Anders Hejlsberg's team did with LINQ was really neat. The whole LINQ thing is just a bunch of general purpose additions to the language, they didn't hard-code Sql Server code in the C# compiler. Instead they keep the door open to add LINQ Providers by implementing the `IQueryable` interface. 

They created two Sql providers. [Linq to Sql](http://msdn.microsoft.com/en-us/library/bb425822.aspx) and [Linq to Entities](http://msdn.microsoft.com/en-us/library/bb386964.aspx), both encouraging a database-first approach.

The number of third-party LINQ providers is growing slowly, not because is not perceived as a useful feature, but because the technical challenge that it supposes. 

In Spring'08 we tried to use our ORM with Microsoft Linq to Sql, but our engine's philosophy didn't play well. We had each entity duplicated so half of our business logic was translating Signum Entities to LINQ proxy classes back an forth. Also, as we usually do applications with a lot of entities, we ended up with huge dbml files hard to maintain. Then we took the hard way, building our own Linq provider that speaks straight in terms of our entities. 

We're proud to have a full LINQ provider since Mid 2008, and we have invested lots of time and resources in improving it since them: 

* **Mid 2008:** Initial LINQ implementation, already with all the operators including `GroupBy` etc...
* **Late 2008:** Support of polymorphic foreign keys (cast, `is` and `as`)
* **Mid 2009:** The `Retriever` is deprecated and the LINQ provider is also responsible for retrieving entities.
* **Late 2008:** Polymorphism using Switch.
* **Early 2010:** Correlated sub-queries to avoid N+1 problem.
* **Early 2010:** Support of `Sytem.Type`.
* **Late 2011:** `UnsafeUpdate` and `UnsafeDelete`.
* **Mid 2012:** The `Deleter` is deprecated and the LINQ provider is responsible for deleting entities.
* **Late 2012:** Better support of polymorphism inheritance using Union or Switch support.
* **Early 2012:** Mixin support.
* **Late 2013:** `UnsafeInsert`.



## Getting started with LINQ
**LINQ to Signum** tries to implement LINQ Standard Query Operators in a straightforward way, but stills has some peculiarities.

Here we are going to focus on these peculiarities, because there are plenty of sites already where you can get an explanation of what LINQ is and how the Standard Query Operators work. Just to enumerate some of them: 

* [LINQ Standard Query Operators](http://aspnetresources.com/downloads/linq_standard_query_operators.pdf): A nice 2 pages abstract with all the Linq operators.
* [The LINQ project](http://msdn.microsoft.com/en-us/library/vstudio/bb397926.aspx): Main page of Linq project.
* [101 LINQ Samples](http://code.msdn.microsoft.com/101-LINQ-Samples-3fb9811b): A collection of Linq queries explained.
* [Linq at Wikipedia](http://en.wikipedia.org/wiki/Language_Integrated_Query): Wikipedia has always a good introduction about anything :)
* [Hooked on Linq](http://www.hookedonlinq.com/): A nice Wiki focused on Linq.
* [LINQPad](http://www.linqpad.net/): Really cool tool for writing Linq to Sql queries having a responsive and interactive experience.

So, at this point, you should be a proficient Linq programmer with experience in some of the non-Signum LINQ flavors, and you're eager to know how our provider works. 

Not so fast, first it would be convenient to take a look and get used to the data model we are going to query against in the examples.

## Query\<T>

The first thing to notice is that we didn't generate any `db.Customer` property for each table, instead you have to write `Database.Query<CustomerDN>()` to get the `IQueryable<CustomerDN>` to start querying `CustomerDN` table. You can find out why in [Database page](Database.md). 

Let's see our first example, if you write a query like this:

```C#
 var result = from b in Database.Query<BugDN>()
              group b by b.Status into g
              select new { Status = g.Key, Num = g.Count() };
```

An Sql like this will be sent:

```SQL
SELECT s2.agg1 AS c0, s2.idStatus
FROM (
  (SELECT bdn.idStatus, COUNT(*) AS agg1
  FROM BugDN AS bdn
  GROUP BY bdn.idStatus)
) AS s2
```

## Retrieving entities

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

In this case, retrieving a `DeveloperDN` was just a few columns because is a simple entity but let's try a bigger entity: 

```C#

```

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

Developer entity is a simple table, but a more complex table will return: 







